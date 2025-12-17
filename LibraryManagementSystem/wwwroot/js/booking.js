function calculateTotal() {
    const startEl = document.getElementById('inputStart');
    const endEl = document.getElementById('inputEnd');
    const rateEl = document.getElementById('hourlyRate');

    if (!startEl || !endEl || !rateEl) return;

    const startVal = startEl.value;
    const endVal = endEl.value;
    const rate = parseFloat(rateEl.value) || 0;

    if (startVal && endVal) {
        const base = "2000-01-01T";
        const s = new Date(base + startVal);
        const e = new Date(base + endVal);

        let diffMs = e - s;
        let hours = diffMs / 1000 / 60 / 60;

        if (hours < 0) hours = 0;

        document.getElementById('displayDuration').innerText = hours.toFixed(1);

        const total = hours * rate;
        document.getElementById('displayPrice').innerText = total.toFixed(2);
    } else {
        document.getElementById('displayDuration').innerText = "0.0";
        document.getElementById('displayPrice').innerText = "0.00";
    }
}

document.addEventListener("DOMContentLoaded", function () {
    calculateTotal();
});