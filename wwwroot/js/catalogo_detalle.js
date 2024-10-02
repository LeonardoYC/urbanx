document.addEventListener('DOMContentLoaded', function () {
    const minusButton = document.querySelector('.minus');
    const plusButton = document.querySelector('.plus');
    const quantityInput = document.getElementById('cantidad');

    minusButton.addEventListener('click', function () {
        let currentValue = parseInt(quantityInput.value);
        if (currentValue > 1) {
            quantityInput.value = currentValue - 1; // Decrementar solo si es mayor a 1
        }
    });

    plusButton.addEventListener('click', function () {
        let currentValue = parseInt(quantityInput.value);
        if (currentValue < 10) { // Limitar a un máximo de 10
            quantityInput.value = currentValue + 1;
        }
    });

    // Para permitir la entrada manual del número
    quantityInput.addEventListener('input', function () {
        let value = parseInt(quantityInput.value);
        if (isNaN(value) || value < 1) {
            quantityInput.value = 1; // Asegura que no sea menor a 1
        } else if (value > 10) {
            quantityInput.value = 10; // Asegura que no sea mayor a 10
        }
    });

    // Inicializar el input con el valor de 1 si está vacío
    quantityInput.addEventListener('blur', function () {
        if (quantityInput.value === '') {
            quantityInput.value = 1; // Asignar 1 si el input está vacío
        }
    });
});