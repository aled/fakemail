// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
var tooltipList = tooltipTriggerList.map(function (element) {
    return new bootstrap.Tooltip(element)
});

document.querySelectorAll('.copyable-text').forEach(item => {
    item.addEventListener('click', () => copyTextToClipboard(item));
});

function copyTextToClipboard(element) {
    navigator.clipboard.writeText(element.innerText);
    const tooltipInstance = bootstrap.Tooltip.getInstance(element);
    if (tooltipInstance) {
        tooltipInstance.hide();
    }
};