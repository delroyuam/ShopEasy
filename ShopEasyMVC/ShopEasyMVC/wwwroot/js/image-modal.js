// Amplía la imagen de un producto al hacer clic, mostrándola en un modal de Bootstrap.
document.addEventListener('DOMContentLoaded', function () {
    var imageModal = document.getElementById('imageModal');
    if (!imageModal) return;

    imageModal.addEventListener('show.bs.modal', function (event) {
        var trigger = event.relatedTarget;
        var img = document.getElementById('imageModalImg');
        img.setAttribute('src', trigger.getAttribute('data-img-src'));
        img.setAttribute('alt', trigger.getAttribute('data-img-alt'));
    });
});
