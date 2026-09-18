// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

// PIN entry: 4 single-digit boxes that behave like one PIN field.
document.addEventListener('DOMContentLoaded', function () {
    document.querySelectorAll('.pin-boxes').forEach(function (container) {
        var inputs = Array.prototype.slice.call(container.querySelectorAll('.pin-digit-input'));
        var hidden = document.getElementById(container.dataset.hiddenTarget);

        inputs.forEach(function (input, index) {
            input.addEventListener('input', function () {
                input.value = input.value.replace(/[^0-9]/g, '').slice(0, 1);
                if (input.value && index < inputs.length - 1) {
                    inputs[index + 1].focus();
                }
            });

            input.addEventListener('keydown', function (e) {
                if (e.key === 'Backspace' && !input.value && index > 0) {
                    inputs[index - 1].focus();
                }
            });
        });

        var form = container.closest('form');
        if (form && hidden) {
            form.addEventListener('submit', function () {
                hidden.value = inputs.map(function (i) { return i.value; }).join('');
            });
        }
    });
});
