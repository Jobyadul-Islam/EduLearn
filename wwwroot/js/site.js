// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// ---------- Toasts: auto-show any server-rendered toast in the layout's toast container ----------
(function () {
    document.querySelectorAll('#toastContainer .toast').forEach(function (el) {
        new bootstrap.Toast(el).show();
    });
})();

// ---------- Loading spinner: disables the submit button on any valid form submission ----------
(function () {
    document.addEventListener('submit', function (e) {
        var form = e.target;
        if (!(form instanceof HTMLFormElement)) return;
        if (form.hasAttribute('data-no-spinner')) return;
        if (typeof form.checkValidity === 'function' && !form.checkValidity()) return;

        var submitBtn = form.querySelector('button[type="submit"]');
        if (!submitBtn || submitBtn.disabled) return;

        var originalText = submitBtn.innerHTML;
        submitBtn.disabled = true;
        submitBtn.innerHTML = '<span class="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>' + originalText;

        // Safety net: if something (e.g. a jQuery-validate rule not covered by
        // checkValidity, like a mismatched confirm-password) blocks the submit and
        // the page never navigates away, don't leave the button stuck forever.
        setTimeout(function () {
            if (document.body.contains(submitBtn)) {
                submitBtn.disabled = false;
                submitBtn.innerHTML = originalText;
            }
        }, 8000);
    }, true);
})();

// ---------- Chat widget (backed by /Chat/SendMessage, Gemini-powered) ----------
(function () {
    var launcher = document.getElementById('chatLauncher');
    var panel = document.getElementById('chatPanel');
    var closeBtn = document.getElementById('chatCloseBtn');
    var body = document.getElementById('chatBody');
    var input = document.getElementById('chatInput');
    var sendBtn = document.getElementById('chatSendBtn');

    if (!launcher || !panel) return;

    function toggleChat(open) {
        panel.classList.toggle('open', open);
        if (open) {
            input.focus();
        }
    }

    launcher.addEventListener('click', function () {
        toggleChat(!panel.classList.contains('open'));
    });

    closeBtn.addEventListener('click', function () {
        toggleChat(false);
    });

    function appendMessage(text, sender) {
        var msg = document.createElement('div');
        msg.className = 'chat-msg ' + sender;
        msg.textContent = text;
        body.appendChild(msg);
        body.scrollTop = body.scrollHeight;
        return msg;
    }

    function sendMessage() {
        var text = input.value.trim();
        if (!text) return;

        appendMessage(text, 'user');
        input.value = '';
        sendBtn.disabled = true;

        var typing = appendMessage('...', 'bot');

        var formData = new URLSearchParams();
        formData.append('message', text);

        fetch('/Chat/SendMessage', {
            method: 'POST',
            headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
            body: formData.toString()
        })
            .then(function (res) { return res.json(); })
            .then(function (data) {
                typing.textContent = data.reply || "Sorry, something went wrong.";
            })
            .catch(function () {
                typing.textContent = "Sorry, I couldn't reach the assistant. Please try again.";
            })
            .finally(function () {
                sendBtn.disabled = false;
                body.scrollTop = body.scrollHeight;
            });
    }

    sendBtn.addEventListener('click', sendMessage);
    input.addEventListener('keydown', function (e) {
        if (e.key === 'Enter') {
            e.preventDefault();
            sendMessage();
        }
    });
})();

// ---------- Profile picture cropper: drag-to-reposition + zoom, like Facebook's uploader ----------
// Any element with [data-picture-cropper] gets wired up automatically. It needs, inside it:
//   [data-cropper-file-input]  <input type="file">
//   [data-cropper-stage]       wrapper shown only once a file is picked
//   [data-cropper-viewport]    the fixed circular frame
//   [data-cropper-image]       the <img> dragged/zoomed inside the frame
//   [data-cropper-zoom]        a <input type="range"> zoom slider
//   [data-cropper-output]      a hidden <input> that receives the final cropped JPEG as a data URL
(function () {
    var VIEWPORT_SIZE = 180; // must match the CSS width/height on [data-cropper-viewport]
    var OUTPUT_SIZE = 400;   // final square image resolution saved to disk

    document.querySelectorAll('[data-picture-cropper]').forEach(function (root) {
        var fileInput = root.querySelector('[data-cropper-file-input]');
        var stage = root.querySelector('[data-cropper-stage]');
        var viewport = root.querySelector('[data-cropper-viewport]');
        var img = root.querySelector('[data-cropper-image]');
        var zoomSlider = root.querySelector('[data-cropper-zoom]');
        var output = root.querySelector('[data-cropper-output]');
        var errorEl = root.querySelector('[data-cropper-error]');
        if (!fileInput || !stage || !viewport || !img || !zoomSlider || !output) return;

        var naturalW = 0, naturalH = 0, baseScale = 1, scale = 1, x = 0, y = 0;
        var dragging = false, startX = 0, startY = 0;

        function clamp(val, min, max) { return Math.max(min, Math.min(max, val)); }

        function applyTransform() {
            var w = naturalW * scale;
            var h = naturalH * scale;
            x = clamp(x, VIEWPORT_SIZE - w, 0);
            y = clamp(y, VIEWPORT_SIZE - h, 0);
            img.style.width = w + 'px';
            img.style.height = h + 'px';
            img.style.left = x + 'px';
            img.style.top = y + 'px';
        }

        function updateOutput() {
            var srcSize = VIEWPORT_SIZE / scale;
            var srcX = -x / scale;
            var srcY = -y / scale;

            var canvas = document.createElement('canvas');
            canvas.width = OUTPUT_SIZE;
            canvas.height = OUTPUT_SIZE;
            canvas.getContext('2d').drawImage(img, srcX, srcY, srcSize, srcSize, 0, 0, OUTPUT_SIZE, OUTPUT_SIZE);
            output.value = canvas.toDataURL('image/jpeg', 0.9);
        }

        fileInput.addEventListener('change', function () {
            if (errorEl) errorEl.textContent = '';
            output.value = '';
            var file = fileInput.files[0];
            if (!file) { stage.classList.add('d-none'); return; }

            var allowed = ['image/jpeg', 'image/png', 'image/webp'];
            if (allowed.indexOf(file.type) === -1) {
                if (errorEl) errorEl.textContent = 'Only JPG, PNG, or WEBP images are allowed.';
                fileInput.value = '';
                stage.classList.add('d-none');
                return;
            }
            if (file.size > 5 * 1024 * 1024) {
                if (errorEl) errorEl.textContent = 'Please choose an image under 5MB.';
                fileInput.value = '';
                stage.classList.add('d-none');
                return;
            }

            var reader = new FileReader();
            reader.onload = function (e) {
                img.onload = function () {
                    naturalW = img.naturalWidth;
                    naturalH = img.naturalHeight;
                    baseScale = Math.max(VIEWPORT_SIZE / naturalW, VIEWPORT_SIZE / naturalH);
                    scale = baseScale;
                    x = (VIEWPORT_SIZE - naturalW * scale) / 2;
                    y = (VIEWPORT_SIZE - naturalH * scale) / 2;
                    zoomSlider.value = 100;
                    applyTransform();
                    updateOutput();
                    stage.classList.remove('d-none');
                };
                img.src = e.target.result;
            };
            reader.readAsDataURL(file);
        });

        zoomSlider.addEventListener('input', function () {
            if (!naturalW) return;
            var newScale = baseScale * (zoomSlider.value / 100);
            // Keep the point currently at the viewport's center anchored while zooming.
            var centerX = VIEWPORT_SIZE / 2, centerY = VIEWPORT_SIZE / 2;
            var imgPointX = (centerX - x) / scale;
            var imgPointY = (centerY - y) / scale;
            scale = newScale;
            x = centerX - imgPointX * scale;
            y = centerY - imgPointY * scale;
            applyTransform();
            updateOutput();
        });

        function startDrag(clientX, clientY) {
            if (!naturalW) return;
            dragging = true;
            startX = clientX - x;
            startY = clientY - y;
            viewport.style.cursor = 'grabbing';
        }
        function moveDrag(clientX, clientY) {
            if (!dragging) return;
            x = clientX - startX;
            y = clientY - startY;
            applyTransform();
        }
        function endDrag() {
            if (!dragging) return;
            dragging = false;
            viewport.style.cursor = 'grab';
            updateOutput();
        }

        viewport.addEventListener('mousedown', function (e) { e.preventDefault(); startDrag(e.clientX, e.clientY); });
        window.addEventListener('mousemove', function (e) { moveDrag(e.clientX, e.clientY); });
        window.addEventListener('mouseup', endDrag);

        viewport.addEventListener('touchstart', function (e) { startDrag(e.touches[0].clientX, e.touches[0].clientY); }, { passive: true });
        viewport.addEventListener('touchmove', function (e) { moveDrag(e.touches[0].clientX, e.touches[0].clientY); }, { passive: true });
        viewport.addEventListener('touchend', endDrag);
    });
})();
