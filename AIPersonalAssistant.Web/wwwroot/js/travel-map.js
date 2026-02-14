// Travel Map JavaScript

let map;
let markers = [];
let pins = [];

// Initialize map
function initMap() {
    map = L.map('map').setView([20, 0], 2);
    
    L.tileLayer('https://tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors',
        maxZoom: 19
    }).addTo(map);
    
    // Click on map to add pin
    map.on('click', (e) => {
        openAddPinModal(e.latlng.lat, e.latlng.lng);
    });
}

// Load pins from API
async function loadPins() {
    try {
        const response = await fetch('/api/travel/pins', {
            credentials: 'include'
        });
        
        if (!response.ok) {
            if (response.status === 401) {
                window.location.href = '/login.html';
                return;
            }
            throw new Error('Failed to load pins');
        }
        
        pins = await response.json();
        renderPins();
    } catch (error) {
        console.error('Error loading pins:', error);
    }
}

// Render pins on map
function renderPins() {
    // Clear existing markers
    markers.forEach(marker => map.removeLayer(marker));
    markers = [];
    
    pins.forEach(pin => {
        const marker = createMarker(pin);
        markers.push(marker);
        marker.addTo(map);
    });
}

// Create a marker for a pin
function createMarker(pin) {
    const icon = L.divIcon({
        className: 'custom-pin-wrapper',
        html: '<div class="custom-pin"><div class="custom-pin-inner"></div></div>',
        iconSize: [30, 30],
        iconAnchor: [15, 30],
        popupAnchor: [0, -30]
    });
    
    const marker = L.marker([pin.latitude, pin.longitude], { icon });
    
    const dateStr = pin.dateVisited ? new Date(pin.dateVisited).toLocaleDateString() : 'Not specified';
    const images = pin.imageUrls || [];
    const maxThumbs = 3;
    const thumbsToShow = images.slice(0, maxThumbs);
    const remaining = images.length - maxThumbs;
    let thumbsHtml = '';
    if (thumbsToShow.length > 0) {
        const thumbItems = thumbsToShow.map(imgId =>
            `<img src="/api/travel/pins/${pin.id}/images/${imgId}" class="popup-thumb" onclick="openImageLightbox('/api/travel/pins/${pin.id}/images/${imgId}')" alt="photo" />`
        ).join('');
        thumbsHtml = `<div class="popup-thumbs">${thumbItems}${remaining > 0 ? `<span class="popup-thumbs-more">+${remaining} more</span>` : ''}</div>`;
    }
    const popupContent = `
        <div class="pin-popup">
            <h3>${escapeHtml(pin.placeName || 'Unnamed Location')}</h3>
            <p><strong>Date:</strong> ${dateStr}</p>
            ${pin.notes ? `<p>${escapeHtml(pin.notes)}</p>` : ''}
            ${thumbsHtml}
            <span class="edit-link" onclick="openEditPinModal('${pin.id}')">✏️ Edit</span>
        </div>
    `;
    
    marker.bindPopup(popupContent);
    marker.pinId = pin.id;
    
    return marker;
}

// Open modal to add new pin
function openAddPinModal(lat, lng) {
    document.getElementById('modalTitle').textContent = 'Add New Pin';
    document.getElementById('pinId').value = '';
    document.getElementById('pinLat').value = lat;
    document.getElementById('pinLng').value = lng;
    document.getElementById('placeName').value = '';
    document.getElementById('dateVisited').value = '';
    document.getElementById('notes').value = '';
    document.getElementById('deleteBtn').style.display = 'none';
    document.getElementById('pinModal').style.display = 'flex';
    configureImageSection(null);
}

// Open modal to edit existing pin
function openEditPinModal(pinId) {
    const pin = pins.find(p => p.id === pinId);
    if (!pin) return;
    
    document.getElementById('modalTitle').textContent = 'Edit Pin';
    document.getElementById('pinId').value = pin.id;
    document.getElementById('pinLat').value = pin.latitude;
    document.getElementById('pinLng').value = pin.longitude;
    document.getElementById('placeName').value = pin.placeName || '';
    document.getElementById('dateVisited').value = pin.dateVisited ? pin.dateVisited.split('T')[0] : '';
    document.getElementById('notes').value = pin.notes || '';
    document.getElementById('deleteBtn').style.display = 'block';
    document.getElementById('pinModal').style.display = 'flex';
    configureImageSection(pinId);
    
    // Close any open popups
    map.closePopup();
}

// Close modal
function closeModal() {
    document.getElementById('pinModal').style.display = 'none';
}

// Save pin (create or update)
async function savePin(e) {
    e.preventDefault();
    
    const saveBtn = document.getElementById('saveBtn');
    const deleteBtn = document.getElementById('deleteBtn');
    const cancelBtn = document.getElementById('cancelBtn');
    const originalText = saveBtn.textContent;
    
    saveBtn.textContent = 'Saving...';
    saveBtn.disabled = true;
    deleteBtn.disabled = true;
    cancelBtn.disabled = true;
    
    const pinId = document.getElementById('pinId').value;
    const pinData = {
        latitude: parseFloat(document.getElementById('pinLat').value),
        longitude: parseFloat(document.getElementById('pinLng').value),
        placeName: document.getElementById('placeName').value,
        dateVisited: document.getElementById('dateVisited').value || null,
        notes: document.getElementById('notes').value
    };
    
    try {
        const url = pinId ? `/api/travel/pins/${pinId}` : '/api/travel/pins';
        const method = pinId ? 'PUT' : 'POST';
        
        const response = await fetch(url, {
            method: method,
            headers: {
                'Content-Type': 'application/json'
            },
            credentials: 'include',
            body: JSON.stringify(pinData)
        });
        
        if (!response.ok) {
            if (response.status === 401) {
                window.location.href = '/login.html';
                return;
            }
            throw new Error('Failed to save pin');
        }
        
        closeModal();
        await loadPins();
    } catch (error) {
        console.error('Error saving pin:', error);
        alert('Failed to save pin. Please try again.');
    } finally {
        saveBtn.textContent = originalText;
        saveBtn.disabled = false;
        deleteBtn.disabled = false;
        cancelBtn.disabled = false;
    }
}

// Delete pin
async function deletePin() {
    const pinId = document.getElementById('pinId').value;
    if (!pinId) return;
    
    if (!confirm('Are you sure you want to delete this pin?')) {
        return;
    }
    
    const deleteBtn = document.getElementById('deleteBtn');
    const saveBtn = document.getElementById('saveBtn');
    const cancelBtn = document.getElementById('cancelBtn');
    const originalText = deleteBtn.textContent;
    
    deleteBtn.textContent = 'Deleting...';
    deleteBtn.disabled = true;
    saveBtn.disabled = true;
    cancelBtn.disabled = true;
    
    try {
        const response = await fetch(`/api/travel/pins/${pinId}`, {
            method: 'DELETE',
            credentials: 'include'
        });
        
        if (!response.ok) {
            if (response.status === 401) {
                window.location.href = '/login.html';
                return;
            }
            throw new Error('Failed to delete pin');
        }
        
        closeModal();
        await loadPins();
    } catch (error) {
        console.error('Error deleting pin:', error);
        alert('Failed to delete pin. Please try again.');
    } finally {
        deleteBtn.textContent = originalText;
        deleteBtn.disabled = false;
        saveBtn.disabled = false;
        cancelBtn.disabled = false;
    }
}

// --- Image Upload ---

function configureImageSection(pinId) {
    const gallery = document.getElementById('imageGallery');
    const dropZone = document.getElementById('dropZone');
    const hint = document.getElementById('imageSaveHint');
    const status = document.getElementById('uploadStatus');
    gallery.innerHTML = '';
    status.style.display = 'none';

    if (!pinId) {
        dropZone.classList.add('disabled');
        hint.style.display = 'block';
    } else {
        dropZone.classList.remove('disabled');
        hint.style.display = 'none';
        loadPinImages(pinId);
    }
}

async function loadPinImages(pinId) {
    const gallery = document.getElementById('imageGallery');
    gallery.innerHTML = '';
    try {
        const res = await fetch(`/api/travel/pins/${pinId}`, { credentials: 'include' });
        if (!res.ok) return;
        const pin = await res.json();
        const images = pin.imageUrls || [];
        images.forEach(imgId => {
            const thumb = document.createElement('div');
            thumb.className = 'image-thumb';
            thumb.innerHTML = `<img src="/api/travel/pins/${pinId}/images/${imgId}" alt="pin image"><button class="remove-img" title="Remove">&times;</button>`;
            thumb.querySelector('.remove-img').addEventListener('click', () => removeImage(pinId, imgId));
            gallery.appendChild(thumb);
        });
        updateDropZoneState(images.length);
    } catch (e) {
        console.error('Error loading images:', e);
    }
}

function updateDropZoneState(count) {
    const dropZone = document.getElementById('dropZone');
    const pinId = document.getElementById('pinId').value;
    if (!pinId) return;
    if (count >= 5) {
        dropZone.classList.add('disabled');
    } else {
        dropZone.classList.remove('disabled');
    }
}

async function uploadImages(files) {
    const pinId = document.getElementById('pinId').value;
    if (!pinId) return;

    const gallery = document.getElementById('imageGallery');
    const currentCount = gallery.querySelectorAll('.image-thumb').length;
    const allowed = 5 - currentCount;
    if (allowed <= 0) { alert('Maximum 5 images per pin.'); return; }

    const toUpload = Array.from(files).slice(0, allowed);
    const status = document.getElementById('uploadStatus');
    status.style.display = 'block';

    for (let i = 0; i < toUpload.length; i++) {
        status.textContent = `Uploading ${i + 1}/${toUpload.length}...`;
        const formData = new FormData();
        formData.append('file', toUpload[i]);
        try {
            const uploadRes = await fetch(`/api/travel/pins/${pinId}/images`, {
                method: 'POST',
                credentials: 'include',
                body: formData
            });
            if (!uploadRes.ok) {
                console.error('Upload failed with status:', uploadRes.status);
            }
        } catch (e) {
            console.error('Upload failed:', e);
        }
    }

    status.style.display = 'none';
    await loadPinImages(pinId);
}

async function removeImage(pinId, imageId) {
    try {
        await fetch(`/api/travel/pins/${pinId}/images/${imageId}`, {
            method: 'DELETE',
            credentials: 'include'
        });
        await loadPinImages(pinId);
    } catch (e) {
        console.error('Error removing image:', e);
    }
}

function initDropZone() {
    const dropZone = document.getElementById('dropZone');
    const fileInput = document.getElementById('fileInput');

    dropZone.addEventListener('click', () => {
        if (!dropZone.classList.contains('disabled')) fileInput.click();
    });

    fileInput.addEventListener('change', () => {
        if (fileInput.files.length) uploadImages(fileInput.files);
        fileInput.value = '';
    });

    dropZone.addEventListener('dragover', (e) => {
        e.preventDefault();
        if (!dropZone.classList.contains('disabled')) dropZone.classList.add('drag-over');
    });

    dropZone.addEventListener('dragleave', () => {
        dropZone.classList.remove('drag-over');
    });

    dropZone.addEventListener('drop', (e) => {
        e.preventDefault();
        dropZone.classList.remove('drag-over');
        if (dropZone.classList.contains('disabled')) return;
        const files = e.dataTransfer.files;
        if (files.length) uploadImages(files);
    });
}

// Escape HTML to prevent XSS
function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

// Open a simple lightbox for a full-size image
function openImageLightbox(src) {
    const overlay = document.createElement('div');
    overlay.className = 'image-lightbox-overlay';
    overlay.innerHTML = `<img src="${src}" class="image-lightbox-img" />`;
    overlay.addEventListener('click', () => overlay.remove());
    document.body.appendChild(overlay);
}

// Event listeners
document.getElementById('backBtn').addEventListener('click', () => {
    window.location.href = '/tools.html';
});

document.querySelector('.modal-close').addEventListener('click', closeModal);
document.getElementById('cancelBtn').addEventListener('click', closeModal);
document.getElementById('pinForm').addEventListener('submit', savePin);
document.getElementById('deleteBtn').addEventListener('click', deletePin);

// Close modal on outside click
document.getElementById('pinModal').addEventListener('click', (e) => {
    if (e.target.id === 'pinModal') {
        closeModal();
    }
});

// Initialize
initMap();
initDropZone();
loadPins();
