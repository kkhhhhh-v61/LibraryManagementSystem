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

window.addEventListener('click', (e) => {
    const editModal = document.getElementById('roomEditModal');
    const addModal = document.getElementById('addRoomModal');
    const dModal = document.getElementById(deleteModalId);

    if (editModal && e.target === editModal) closeEditModal();
    if (addModal && e.target === addModal) closeAddRoomModal();
    if (dModal && e.target === dModal) closeDeleteModal();
});

window.addEventListener('keydown', (e) => {
    if (e.key === 'Escape') {
        closeEditModal();
        closeAddRoomModal();
        closeDeleteModal();
    }

    if (e.altKey && e.key.toLowerCase() === 'c') {
        e.preventDefault();
        openAddRoomModal();
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

function addCategoryToDropdown(data) {
    var select = document.getElementById('RoomInput_RoomCategoryId');
    if (select) {
        var option = new Option(data.name, data.id);
        select.add(option, 1);
        select.style.borderColor = "var(--success)";
        setTimeout(() => select.style.borderColor = "", 1000);
    }
}

let categoryCropper = null;
const MAX_FILE_SIZE = 1024 * 1024;

document.addEventListener('change', function (e) {
    if (e.target && (e.target.id === 'categoryImgInput' || e.target.id === 'editCategoryImgInput')) {
        const files = e.target.files;
        if (files && files.length > 0) {
            const file = files[0];
            const image = document.getElementById('imageToCrop');
            const modal = document.getElementById('cropperModal');

            modal.setAttribute('data-target-input', e.target.id);

            const reader = new FileReader();
            reader.onload = function (event) {
                image.src = event.target.result;
                modal.style.display = 'flex';

                if (categoryCropper) {
                    categoryCropper.destroy();
                }

                categoryCropper = new Cropper(image, {
                    aspectRatio: 16 / 9,
                    viewMode: 1,
                    autoCropArea: 1,
                });
            };
            reader.readAsDataURL(file);
        }
        e.target.value = '';
    }
});

document.addEventListener('click', function (e) {
    if (e.target && e.target.id === 'btnCropConfirm') {
        const modal = document.getElementById('cropperModal');
        const targetInputId = modal.getAttribute('data-target-input');
        const input = document.getElementById(targetInputId);

        if (categoryCropper && input) {
            e.target.innerText = "Compressing...";
            e.target.disabled = true;

            const canvas = categoryCropper.getCroppedCanvas({
                maxWidth: 1920,
                maxHeight: 1080,
                imageSmoothingQuality: 'high'
            });

            compressImage(canvas, 1.0, function (blob) {
                const newFile = new File([blob], "cropped-image.jpg", { type: "image/jpeg" });
                const dataTransfer = new DataTransfer();
                dataTransfer.items.add(newFile);
                input.files = dataTransfer.files;

                const previewImg = document.querySelector('.img-preview-box img');
                if (previewImg) {
                    previewImg.src = URL.createObjectURL(blob);
                }

                e.target.innerText = "Crop & Save";
                e.target.disabled = false;
                closeCropperModal();
                console.log(`Final Image Size: ${(blob.size / 1024).toFixed(2)} KB`);
            });
        }
    }
});

function compressImage(canvas, quality, callback) {
    canvas.toBlob(function (blob) {
        if (blob.size <= MAX_FILE_SIZE || quality <= 0.6) {
            callback(blob);
        }
        else {
            const newQuality = parseFloat((quality - 0.1).toFixed(1));
            compressImage(canvas, newQuality, callback);
        }
    }, 'image/jpeg', quality);
}

function closeCropperModal() {
    const modal = document.getElementById('cropperModal');
    if (modal) {
        modal.style.display = 'none';
    }
    if (categoryCropper) {
        categoryCropper.destroy();
        categoryCropper = null;
    }
}

let searchTimeout;

function submitSearch() {
    clearTimeout(searchTimeout);
    searchTimeout = setTimeout(function () {
        $("#searchForm").submit();
    }, 500);
}

let roomSearchTimeout;

function submitRoomSearch() {
    clearTimeout(roomSearchTimeout);
    roomSearchTimeout = setTimeout(function () {
        $("#roomSearchForm").submit();
    }, 500);
}

document.addEventListener("DOMContentLoaded", function () {
    if (window.serverSideHasErrors && window.serverSideActiveModal) {
        var modal = document.getElementById(window.serverSideActiveModal);
        if (modal) {
            modal.style.display = 'block';
        }
    }
});