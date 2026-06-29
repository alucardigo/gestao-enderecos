// Modal de confirmação de exclusão, genérico para qualquer listagem.
// Uso na view:
//   <div class="modal" data-delete-modal> com um <form data-delete-action="/Controller/Delete">
//   e um elemento [data-delete-target] para o texto; cada botão informa data-id e data-label.
document.querySelectorAll('[data-delete-modal]').forEach(function (modal) {
    modal.addEventListener('show.bs.modal', function (event) {
        const botao = event.relatedTarget;
        if (!botao) return;

        const alvo = modal.querySelector('[data-delete-target]');
        if (alvo) alvo.textContent = botao.getAttribute('data-label') || '';

        const form = modal.querySelector('form[data-delete-action]');
        if (form) {
            const base = form.getAttribute('data-delete-action').replace(/\/$/, '');
            form.action = base + '/' + botao.getAttribute('data-id');
        }
    });
});
