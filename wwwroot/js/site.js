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
