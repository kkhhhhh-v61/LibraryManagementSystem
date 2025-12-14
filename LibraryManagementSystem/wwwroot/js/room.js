const modalId = 'roomEditModal';
const addRoomModalId = 'addRoomModal';
const deleteModalId = 'deleteConfirmationModal';

function openAddRoomModal() {
    const modal = document.getElementById(addRoomModalId);
    if (modal) {
        modal.style.display = 'block';

        const form = modal.querySelector('form');
        if (form) form.reset();
    }
}

function closeAddRoomModal() {
    const modal = document.getElementById(addRoomModalId);
    if (modal) {
        modal.style.display = 'none';
    }
}

function openEditModal(roomId, name, location, capacity, status) {
    const modal = document.getElementById(modalId);

    if (!modal) {
        console.error('Modal not found!');
        return;
    }

    document.getElementById('modalRoomIdHidden').value = roomId;
    document.getElementById('modalRoomIdDisplay').value = roomId;

    document.getElementById('modalRoomName').value = name;
    document.getElementById('modalRoomLocation').value = location;

    document.getElementById('modalCapacity').value = capacity;
    document.getElementById('modalStatus').value = status;

    modal.style.display = 'block';
}

function closeEditModal() {
    const modal = document.getElementById(modalId);
    if (modal) {
        modal.style.display = 'none';
    }
}

// Close on click outside
window.addEventListener('click', (e) => {
    const editModal = document.getElementById('roomEditModal');
    const addModal = document.getElementById('addRoomModal');

    if (editModal && e.target === editModal) {
        closeEditModal();
    }

    if (addModal && e.target === addModal) {
        closeAddRoomModal();
    }
});

// Close on Escape key
window.addEventListener('keydown', (e) => {
    if (e.key === 'Escape') {
        closeEditModal();
        closeAddRoomModal();
    }
});

function openDeleteModal(actionUrl, itemName) {
    const modal = document.getElementById(deleteModalId);
    if (!modal) return;

    const deleteForm = document.getElementById('deleteForm');
    deleteForm.action = actionUrl;

    document.getElementById('deleteItemName').textContent = itemName;

    modal.style.display = 'block';
}

function closeDeleteModal() {
    const modal = document.getElementById(deleteModalId);
    if (modal) modal.style.display = 'none';
}

window.addEventListener('click', (e) => {
    const dModal = document.getElementById(deleteModalId);
    if (e.target === dModal) closeDeleteModal();
});

function submitCategory(e) {
    e.preventDefault();

    var form = document.getElementById('formCreateCategory');
    var formData = new FormData(form);

    clearErrors(form);

    fetch('/Room/CreateCategoryAjax', {
        method: 'POST',
        body: formData
    })
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                showToast(data.message, 'success');
                form.reset();

                addCategoryToTable(data.data);

                addCategoryToDropdown(data.data);
            } else {
                showToast('Please fix the errors.', 'error');
                displayErrors(form, data.errors);
            }
        })
        .catch(error => console.error('Error:', error));
}

function submitRoom(e) {
    e.preventDefault(); 

    var form = document.getElementById('formCreateRoom');
    var formData = new FormData(form);

    clearErrors(form);

    fetch('/Room/CreateRoomAjax', {
        method: 'POST',
        body: formData
    })
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                showToast(data.message, 'success');
                form.reset();
            } else {
                showToast('Failed to add room.', 'error');
                displayErrors(form, data.errors);
            }
        })
        .catch(error => console.error('Error:', error));
}

function clearErrors(form) {
    var spans = form.querySelectorAll('.text-danger');
    spans.forEach(s => s.textContent = '');
}

function displayErrors(form, errors) {
    for (var key in errors) {
        var span = form.querySelector(`span[data-valmsg-for="${key}"]`);
        if (span) {
            span.textContent = errors[key][0];
        }
    }
}

function addCategoryToTable(data) {
    var tbody = document.querySelector('.modern-table tbody');

    if (!tbody) return;

    var newRow = `
        <tr>
            <td><code>${data.id}</code></td>
            <td><strong>${data.name}</strong></td>
            <td>RM ${data.rate}</td>
            <td style="color:var(--text-muted);">${data.description}</td>
            <td style="text-align:right;">
                <a href="/Room/Edit/${data.id}" class="btn-icon" title="View Details">
                    <img src="/images/icons/edit.svg" />
                </a>
            </td>
        </tr>
    `;

    tbody.insertAdjacentHTML('afterbegin', newRow);

    var badge = document.querySelector('.badge-neutral');
    if (badge) {
        var currentCount = parseInt(badge.innerText) || 0;
        badge.innerText = (currentCount + 1) + " Records";
    }
}

function addCategoryToDropdown(data) {
    var select = document.getElementById('RoomInput_RoomCategoryId');

    if (select) {
        var option = new Option(data.name, data.id);
        select.add(option, 1);

        select.style.borderColor = "var(--success)";
        setTimeout(() => select.style.borderColor = "", 1000);
    }
}
