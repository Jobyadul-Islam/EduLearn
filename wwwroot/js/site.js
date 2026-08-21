// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

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
