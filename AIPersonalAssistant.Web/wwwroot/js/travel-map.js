// Travel Map JavaScript

let map;
let markers = [];
let pins = [];

// Initialize map
function initMap() {
    map = L.map('map').setView([20, 0], 2);
    
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '© <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors',
        maxZoom: 18
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
    const popupContent = `
        <div class="pin-popup">
            <h3>${escapeHtml(pin.placeName || 'Unnamed Location')}</h3>
            <p><strong>Date:</strong> ${dateStr}</p>
            ${pin.notes ? `<p>${escapeHtml(pin.notes)}</p>` : ''}
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
    }
}

// Delete pin
async function deletePin() {
    const pinId = document.getElementById('pinId').value;
    if (!pinId) return;
    
    if (!confirm('Are you sure you want to delete this pin?')) {
        return;
    }
    
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
    }
}

// Escape HTML to prevent XSS
function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
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
loadPins();
