// Autocompleta el precio unitario con el precio del producto seleccionado.
document.addEventListener('DOMContentLoaded', function () {
    var productSelect = document.getElementById('ProductId');
    var priceInput = document.getElementById('UnitPrice');
    if (!productSelect || !priceInput) return;

    var prices = {};
    try {
        prices = JSON.parse(productSelect.getAttribute('data-product-prices') || '{}');
    } catch (e) {
        prices = {};
    }

    productSelect.addEventListener('change', function () {
        var price = prices[productSelect.value];
        if (price !== undefined && price !== null) {
            priceInput.value = price;
        }
    });
});
