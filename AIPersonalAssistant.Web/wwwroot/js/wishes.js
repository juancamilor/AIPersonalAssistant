let wishes = [];
let deleteTargetId = null;

// Auth check
async function checkAuth() {
    try {
        const response = await fetch('/api/auth/user', { credentials: 'include' });
        if (!response.ok) {
            window.location.href = '/login.html';
            return null;
        }
        const user = await response.json();
        document.getElementById('userEmail').textContent = user.email;
        return user;
    } catch (error) {
        console.error('Auth check failed:', error);
        window.location.href = '/login.html';
        return null;
    }
}

// Load wishes
async function loadWishes() {
    try {
        const response = await fetch('/api/wishes', { credentials: 'include' });

        if (!response.ok) {
            if (response.status === 401) {
                window.location.href = '/login.html';
                return;
            }
            throw new Error('Failed to load wishes');
        }

        wishes = await response.json();
        renderWishes();
    } catch (error) {
        console.error('Error loading wishes:', error);
        document.getElementById('wishesGrid').innerHTML =
            '<p class="loading-text">Error loading wishes. Please try again.</p>';
    }
}

// Render wish cards
function renderWishes() {
    const grid = document.getElementById('wishesGrid');

    if (wishes.length === 0) {
        grid.innerHTML = '<p class="loading-text">No wishes yet. Click "Add New Wish" to create your first one.</p>';
        return;
    }

    grid.innerHTML = wishes.map(wish => {
        const date = new Date(wish.createdAt).toLocaleDateString();
        return `
        <div class="wish-card" data-id="${wish.id}">
            <h3 class="wish-card-title">${escapeHtml(wish.title)}</h3>
            <p class="wish-card-content">${escapeHtml(wish.content)}</p>
            <div class="wish-card-date">${date}</div>
            <div class="wish-card-actions">
                <button class="btn-icon btn-share" title="Share" onclick="shareWish('${wish.shareToken}')">🔗 Share</button>
                <button class="btn-icon btn-edit" title="Edit" onclick="openEditModal('${wish.id}')">✏️ Edit</button>
                <button class="btn-icon btn-delete" title="Delete" onclick="openDeleteModal('${wish.id}')">🗑️ Delete</button>
            </div>
        </div>`;
    }).join('');
}

// Open add modal
function openAddModal() {
    document.getElementById('modalTitle').textContent = 'Add New Wish';
    document.getElementById('wishId').value = '';
    document.getElementById('wishTitle').value = '';
    document.getElementById('wishContent').value = '';
    document.getElementById('wishModal').style.display = 'flex';
}

// Open edit modal
function openEditModal(id) {
    const wish = wishes.find(w => w.id === id);
    if (!wish) return;

    document.getElementById('modalTitle').textContent = 'Edit Wish';
    document.getElementById('wishId').value = wish.id;
    document.getElementById('wishTitle').value = wish.title;
    document.getElementById('wishContent').value = wish.content;
    document.getElementById('wishModal').style.display = 'flex';
}

// Close wish modal
function closeWishModal() {
    document.getElementById('wishModal').style.display = 'none';
}

// Save wish
async function saveWish(e) {
    e.preventDefault();

    const saveBtn = document.getElementById('saveBtn');
    const originalText = saveBtn.textContent;
    saveBtn.textContent = 'Saving...';
    saveBtn.disabled = true;

    const id = document.getElementById('wishId').value;
    const data = {
        title: document.getElementById('wishTitle').value,
        content: document.getElementById('wishContent').value
    };

    try {
        const url = id ? `/api/wishes/${id}` : '/api/wishes';
        const method = id ? 'PUT' : 'POST';

        const response = await fetch(url, {
            method,
            headers: { 'Content-Type': 'application/json' },
            credentials: 'include',
            body: JSON.stringify(data)
        });

        if (!response.ok) {
            if (response.status === 401) {
                window.location.href = '/login.html';
                return;
            }
            throw new Error('Failed to save wish');
        }

        closeWishModal();
        await loadWishes();
    } catch (error) {
        console.error('Error saving wish:', error);
        alert('Failed to save wish. Please try again.');
    } finally {
        saveBtn.textContent = originalText;
        saveBtn.disabled = false;
    }
}

// Open delete confirmation
function openDeleteModal(id) {
    deleteTargetId = id;
    document.getElementById('deleteModal').style.display = 'flex';
}

// Close delete modal
function closeDeleteModal() {
    deleteTargetId = null;
    document.getElementById('deleteModal').style.display = 'none';
}

// Confirm delete
async function confirmDelete() {
    if (!deleteTargetId) return;

    const btn = document.getElementById('confirmDeleteBtn');
    const originalText = btn.textContent;
    btn.textContent = 'Deleting...';
    btn.disabled = true;

    try {
        const response = await fetch(`/api/wishes/${deleteTargetId}`, {
            method: 'DELETE',
            credentials: 'include'
        });

        if (!response.ok) {
            if (response.status === 401) {
                window.location.href = '/login.html';
                return;
            }
            throw new Error('Failed to delete wish');
        }

        closeDeleteModal();
        await loadWishes();
    } catch (error) {
        console.error('Error deleting wish:', error);
        alert('Failed to delete wish. Please try again.');
    } finally {
        btn.textContent = originalText;
        btn.disabled = false;
    }
}

// Share wish
async function shareWish(shareToken) {
    const url = `${window.location.origin}/shared-wishes.html?token=${shareToken}`;
    try {
        await navigator.clipboard.writeText(url);
        showToast('Link copied to clipboard!');
    } catch {
        // Fallback for older browsers
        const input = document.createElement('input');
        input.value = url;
        document.body.appendChild(input);
        input.select();
        document.execCommand('copy');
        document.body.removeChild(input);
        showToast('Link copied to clipboard!');
    }
}

// Toast notification
function showToast(message) {
    const toast = document.getElementById('toast');
    toast.textContent = message;
    toast.style.display = 'block';
    setTimeout(() => { toast.style.display = 'none'; }, 3000);
}

// Escape HTML
function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

// Event listeners
document.getElementById('backBtn').addEventListener('click', () => {
    window.location.href = '/tools.html';
});

document.getElementById('addWishBtn').addEventListener('click', openAddModal);
document.querySelector('#wishModal .modal-close').addEventListener('click', closeWishModal);
document.getElementById('cancelBtn').addEventListener('click', closeWishModal);
document.getElementById('wishForm').addEventListener('submit', saveWish);

document.getElementById('confirmDeleteBtn').addEventListener('click', confirmDelete);
document.getElementById('cancelDeleteBtn').addEventListener('click', closeDeleteModal);

// Close modals on outside click
document.getElementById('wishModal').addEventListener('click', (e) => {
    if (e.target.id === 'wishModal') closeWishModal();
});
document.getElementById('deleteModal').addEventListener('click', (e) => {
    if (e.target.id === 'deleteModal') closeDeleteModal();
});

// Initialize
(async () => {
    const user = await checkAuth();
    if (!enforceToolPermission(user, 'wishes')) return;
    await loadWishes();
})();
