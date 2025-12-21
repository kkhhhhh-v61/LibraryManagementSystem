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

let searchTimeout;

window.debouncedSearch = function () {
    clearTimeout(searchTimeout);
    searchTimeout = setTimeout(sendSearchRequest, 500);
};

window.sendSearchRequest = function () {
    var searchVal = document.getElementById('searchInput').value;
    var dateVal = document.getElementById('dateInput').value;
    var container = document.getElementById('bookingListContainer');

    if (!container) return;

    container.style.opacity = '0.5';

    fetch(`/Booking/BookingHistory?search=${encodeURIComponent(searchVal)}&date=${encodeURIComponent(dateVal)}`, {
        headers: {
            'X-Requested-With': 'XMLHttpRequest'
        }
    })
        .then(response => response.text())
        .then(html => {
            container.innerHTML = html;
            container.style.opacity = '1';
        })
        .catch(error => {
            console.error('Error:', error);
            container.style.opacity = '1';
        });
};

window.openCancelModal = function (event, bookingId, type, btnElement) {
    event.stopPropagation();
    event.preventDefault();

    const modal = document.getElementById('cancelModal');
    const title = document.getElementById('modalTitle');
    const desc = document.getElementById('modalDesc');
    const confirmBtn = document.getElementById('modalConfirmBtn');
    const inputId = document.getElementById('modalBookingId');
    const reasonSection = document.getElementById('reasonSection');
    const reasonInput = document.getElementById('modalReason');
    const modalDetails = document.getElementById('modalDetails');

    if (!modal) return;

    inputId.value = bookingId;

    if (btnElement) {
        document.getElementById('displayRoom').innerText = btnElement.getAttribute('data-room');
        document.getElementById('displayCat').innerText = btnElement.getAttribute('data-cat');
        document.getElementById('displayTime').innerText = btnElement.getAttribute('data-date') + ' | ' + btnElement.getAttribute('data-time');
        modalDetails.style.display = 'block';
    } else {
        modalDetails.style.display = 'none';
    }

    reasonInput.value = '';

    if (type === 'refund') {
        title.innerText = 'Request Refund?';
        desc.innerText = 'Refund is only possible within 30 minutes of payment.';
        confirmBtn.innerText = 'Submit Refund Request';
        confirmBtn.style.backgroundColor = '#d97706';

        reasonSection.style.display = 'block';
        reasonInput.required = true;
        reasonInput.placeholder = "Please explain why you need a refund...";
    }
    else {
        title.innerText = 'Cancel Booking?';
        desc.innerText = 'Are you sure you want to cancel this booking? This action cannot be undone.';
        confirmBtn.innerText = 'Yes, Cancel Booking';
        confirmBtn.style.backgroundColor = '#ef4444';

        reasonSection.style.display = 'none';
        reasonInput.required = false;
    }

    modal.classList.add('active');
};

window.closeModal = function () {
    const modal = document.getElementById('cancelModal');
    if (modal) modal.classList.remove('active');
};

document.addEventListener("DOMContentLoaded", function () {
    calculateTotal();

    const $bookingForm = $('form[asp-action="Create"]');

    if (document.getElementById('inputStart')) {
        $('form').on('submit', function (e) {
            var startVal = $('#inputStart').val();
            var endVal = $('#inputEnd').val();

            if (startVal && endVal) {
                var start = new Date("2000-01-01T" + startVal);
                var end = new Date("2000-01-01T" + endVal);

                var diffMs = end - start;
                var diffMins = diffMs / 1000 / 60;

                if (diffMins > 0 && diffMins < 30) {
                    e.preventDefault();
                    if (window.showToast) {
                        window.showToast('Booking duration must be at least 30 minutes.', 'error');
                    } else {
                        alert('Booking duration must be at least 30 minutes.');
                    }
                    return false;
                }
            }
        });
    }

    const modalReason = document.getElementById('modalReason');
    const modalForm = document.getElementById('modalForm');
    const cancelModal = document.getElementById('cancelModal');

    if (modalReason) {
        modalReason.addEventListener('input', function (e) {
            const currentLength = e.target.value.length;
            const charCount = document.getElementById('charCount');
            if (charCount) charCount.innerText = `${currentLength}/250`;

            if (currentLength > 0 && currentLength < 5) {
                e.target.style.borderColor = '#ef4444';
            } else {
                e.target.style.borderColor = '#cbd5e1';
            }
        });
    }

    if (modalForm) {
        modalForm.addEventListener('submit', function (e) {
            const reasonSection = document.getElementById('reasonSection');
            const reasonInput = document.getElementById('modalReason');

            if (reasonSection && reasonSection.style.display !== 'none') {
                const val = reasonInput.value.trim();
                if (val.length < 5) {
                    e.preventDefault();
                    if (window.showToast) {
                        window.showToast('Reason is too short (min 5 chars).', 'error');
                    }
                    reasonInput.focus();
                }
            }
        });
    }

    if (cancelModal) {
        cancelModal.addEventListener('click', function (e) {
            if (e.target === this) closeModal();
        });
    }
});