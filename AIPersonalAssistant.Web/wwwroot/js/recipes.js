// Cooking Recipes JavaScript

let allRecipes = [];
let categories = [];
let deleteTargetId = null;

// --- Auth ---
async function checkAuth() {
    try {
        const response = await fetch('/api/auth/user', { credentials: 'include' });
        if (!response.ok) { window.location.href = '/login.html'; return null; }
        const user = await response.json();
        document.getElementById('userEmail').textContent = user.email;
        return user;
    } catch (error) {
        console.error('Auth check failed:', error);
        window.location.href = '/login.html';
        return null;
    }
}

// --- Data Loading ---
async function loadRecipes() {
    try {
        const response = await fetch('/api/recipes', { credentials: 'include' });
        if (!response.ok) {
            if (response.status === 401) { window.location.href = '/login.html'; return; }
            throw new Error('Failed to load recipes');
        }
        allRecipes = await response.json();
        filterAndRender();
    } catch (error) {
        console.error('Error loading recipes:', error);
    }
}

async function loadCategories() {
    try {
        const response = await fetch('/api/recipes/categories', { credentials: 'include' });
        if (!response.ok) return;
        categories = await response.json();
        populateCategoryFilter();
        populateCategoryDatalist();
    } catch (error) {
        console.error('Error loading categories:', error);
    }
}

function populateCategoryFilter() {
    const select = document.getElementById('categoryFilter');
    const current = select.value;
    select.innerHTML = '<option value="">All Categories</option>';
    categories.forEach(cat => {
        const opt = document.createElement('option');
        opt.value = cat;
        opt.textContent = cat;
        select.appendChild(opt);
    });
    select.value = current;
}

function populateCategoryDatalist() {
    const datalist = document.getElementById('categoryList');
    datalist.innerHTML = '';
    categories.forEach(cat => {
        const opt = document.createElement('option');
        opt.value = cat;
        datalist.appendChild(opt);
    });
}

// --- Filtering & Rendering ---
function filterAndRender() {
    const query = document.getElementById('searchInput').value.toLowerCase().trim();
    const catFilter = document.getElementById('categoryFilter').value;

    let filtered = allRecipes;

    if (query) {
        filtered = filtered.filter(r =>
            (r.title || '').toLowerCase().includes(query) ||
            (r.ingredients || '').toLowerCase().includes(query) ||
            (r.description || '').toLowerCase().includes(query)
        );
    }

    if (catFilter) {
        filtered = filtered.filter(r => r.category === catFilter);
    }

    renderCards(filtered);
}

function renderCards(recipes) {
    const grid = document.getElementById('recipesGrid');

    if (recipes.length === 0) {
        grid.innerHTML = `
            <div class="empty-state" style="grid-column: 1 / -1;">
                <div class="empty-state-icon">🍽️</div>
                <h3>No recipes found</h3>
                <p>Add your first recipe to get started!</p>
            </div>`;
        return;
    }

    grid.innerHTML = recipes.map(r => {
        const images = r.imageUrls || [];
        const imageHtml = images.length > 0
            ? `<img class="recipe-card-image" src="/api/recipes/${r.id}/images/${images[0]}" alt="${escapeHtml(r.title)}">`
            : `<div class="recipe-card-placeholder">🍳</div>`;

        const categoryHtml = r.category
            ? `<span class="category-badge">${escapeHtml(r.category)}</span>`
            : '';

        const timeHtml = r.prepTime
            ? `<span class="recipe-card-time">⏱️ ${r.prepTime} min prep</span>`
            : '';

        return `
        <div class="recipe-card" data-id="${r.id}">
            ${imageHtml}
            <div class="recipe-card-body">
                <h3 class="recipe-card-title">${escapeHtml(r.title)}</h3>
                <div class="recipe-card-meta">
                    ${categoryHtml}
                    ${timeHtml}
                </div>
                <p class="recipe-card-desc">${escapeHtml(r.description || '')}</p>
                <div class="recipe-card-actions">
                    <button class="btn-card btn-card-edit" onclick="openEditModal('${r.id}')">✏️ Edit</button>
                    <button class="btn-card btn-card-share" onclick="shareRecipe('${r.id}')">🔗 Share</button>
                    <button class="btn-card btn-card-delete" onclick="openDeleteModal('${r.id}')">🗑️</button>
                </div>
            </div>
        </div>`;
    }).join('');
}

// --- Modal: Add/Edit ---
function openAddModal() {
    document.getElementById('modalTitle').textContent = 'Add New Recipe';
    document.getElementById('recipeId').value = '';
    document.getElementById('recipeTitle').value = '';
    document.getElementById('recipeCategory').value = '';
    document.getElementById('recipeDescription').value = '';
    document.getElementById('recipeIngredients').value = '';
    document.getElementById('recipeInstructions').value = '';
    document.getElementById('recipePrepTime').value = '';
    document.getElementById('recipeCookTime').value = '';
    document.getElementById('recipeServings').value = '';
    configureImageSection(null);
    document.getElementById('recipeModal').style.display = 'flex';
}

function openEditModal(recipeId) {
    const recipe = allRecipes.find(r => r.id === recipeId);
    if (!recipe) return;

    document.getElementById('modalTitle').textContent = 'Edit Recipe';
    document.getElementById('recipeId').value = recipe.id;
    document.getElementById('recipeTitle').value = recipe.title || '';
    document.getElementById('recipeCategory').value = recipe.category || '';
    document.getElementById('recipeDescription').value = recipe.description || '';
    document.getElementById('recipeIngredients').value = recipe.ingredients || '';
    document.getElementById('recipeInstructions').value = recipe.instructions || '';
    document.getElementById('recipePrepTime').value = recipe.prepTime || '';
    document.getElementById('recipeCookTime').value = recipe.cookTime || '';
    document.getElementById('recipeServings').value = recipe.servings || '';
    configureImageSection(recipeId);
    document.getElementById('recipeModal').style.display = 'flex';
}

function closeModal() {
    document.getElementById('recipeModal').style.display = 'none';
}

async function saveRecipe(e) {
    e.preventDefault();

    const saveBtn = document.getElementById('saveBtn');
    const cancelBtn = document.getElementById('cancelBtn');
    const originalText = saveBtn.textContent;

    saveBtn.textContent = 'Saving...';
    saveBtn.disabled = true;
    cancelBtn.disabled = true;

    const recipeId = document.getElementById('recipeId').value;
    const data = {
        title: document.getElementById('recipeTitle').value,
        description: document.getElementById('recipeDescription').value,
        category: document.getElementById('recipeCategory').value,
        ingredients: document.getElementById('recipeIngredients').value,
        instructions: document.getElementById('recipeInstructions').value,
        prepTime: document.getElementById('recipePrepTime').value || '',
        cookTime: document.getElementById('recipeCookTime').value || '',
        servings: document.getElementById('recipeServings').value || ''
    };

    try {
        const url = recipeId ? `/api/recipes/${recipeId}` : '/api/recipes';
        const method = recipeId ? 'PUT' : 'POST';

        const response = await fetch(url, {
            method,
            headers: { 'Content-Type': 'application/json' },
            credentials: 'include',
            body: JSON.stringify(data)
        });

        if (!response.ok) {
            if (response.status === 401) { window.location.href = '/login.html'; return; }
            throw new Error('Failed to save recipe');
        }

        // If new recipe, get the id for image upload
        if (!recipeId) {
            const created = await response.json();
            document.getElementById('recipeId').value = created.id;
            configureImageSection(created.id);
            showToast('Recipe created! You can now add images.');
            saveBtn.textContent = originalText;
            saveBtn.disabled = false;
            cancelBtn.disabled = false;
            document.getElementById('modalTitle').textContent = 'Edit Recipe';
            await loadRecipes();
            await loadCategories();
            return;
        }

        closeModal();
        await loadRecipes();
        await loadCategories();
    } catch (error) {
        console.error('Error saving recipe:', error);
        alert('Failed to save recipe. Please try again.');
    } finally {
        saveBtn.textContent = originalText;
        saveBtn.disabled = false;
        cancelBtn.disabled = false;
    }
}

// --- Delete ---
function openDeleteModal(recipeId) {
    deleteTargetId = recipeId;
    document.getElementById('deleteModal').style.display = 'flex';
}

function closeDeleteModal() {
    deleteTargetId = null;
    document.getElementById('deleteModal').style.display = 'none';
}

async function confirmDelete() {
    if (!deleteTargetId) return;

    const btn = document.getElementById('confirmDeleteBtn');
    const originalText = btn.textContent;
    btn.textContent = 'Deleting...';
    btn.disabled = true;

    try {
        const response = await fetch(`/api/recipes/${deleteTargetId}`, {
            method: 'DELETE',
            credentials: 'include'
        });

        if (!response.ok) {
            if (response.status === 401) { window.location.href = '/login.html'; return; }
            throw new Error('Failed to delete recipe');
        }

        closeDeleteModal();
        await loadRecipes();
        await loadCategories();
        showToast('Recipe deleted.');
    } catch (error) {
        console.error('Error deleting recipe:', error);
        alert('Failed to delete recipe. Please try again.');
    } finally {
        btn.textContent = originalText;
        btn.disabled = false;
    }
}

// --- Share ---
async function shareRecipe(recipeId) {
    const recipe = allRecipes.find(r => r.id === recipeId);
    if (!recipe || !recipe.shareToken) {
        showToast('Share link not available.');
        return;
    }

    const url = `${window.location.origin}/shared-recipe.html?token=${recipe.shareToken}`;
    try {
        await navigator.clipboard.writeText(url);
        showToast('Share link copied to clipboard!');
    } catch {
        // Fallback
        const input = document.createElement('input');
        input.value = url;
        document.body.appendChild(input);
        input.select();
        document.execCommand('copy');
        document.body.removeChild(input);
        showToast('Share link copied to clipboard!');
    }
}

// --- Image Upload ---
function configureImageSection(recipeId) {
    const gallery = document.getElementById('imageGallery');
    const dropZone = document.getElementById('dropZone');
    const hint = document.getElementById('imageSaveHint');
    const status = document.getElementById('uploadStatus');
    gallery.innerHTML = '';
    status.style.display = 'none';

    if (!recipeId) {
        dropZone.classList.add('disabled');
        hint.style.display = 'block';
    } else {
        dropZone.classList.remove('disabled');
        hint.style.display = 'none';
        loadRecipeImages(recipeId);
    }
}

async function loadRecipeImages(recipeId) {
    const gallery = document.getElementById('imageGallery');
    gallery.innerHTML = '';
    try {
        const res = await fetch(`/api/recipes/${recipeId}`, { credentials: 'include' });
        if (!res.ok) return;
        const recipe = await res.json();
        const images = recipe.imageUrls || [];
        images.forEach(imgId => {
            const thumb = document.createElement('div');
            thumb.className = 'image-thumb';
            thumb.innerHTML = `<img src="/api/recipes/${recipeId}/images/${imgId}" alt="recipe image"><button class="remove-img" title="Remove">&times;</button>`;
            thumb.querySelector('.remove-img').addEventListener('click', () => removeImage(recipeId, imgId));
            gallery.appendChild(thumb);
        });
        updateDropZoneState(images.length);
    } catch (e) {
        console.error('Error loading images:', e);
    }
}

function updateDropZoneState(count) {
    const dropZone = document.getElementById('dropZone');
    const recipeId = document.getElementById('recipeId').value;
    if (!recipeId) return;
    if (count >= 5) {
        dropZone.classList.add('disabled');
    } else {
        dropZone.classList.remove('disabled');
    }
}

async function uploadImages(files) {
    const recipeId = document.getElementById('recipeId').value;
    if (!recipeId) return;

    const gallery = document.getElementById('imageGallery');
    const currentCount = gallery.querySelectorAll('.image-thumb').length;
    const allowed = 5 - currentCount;
    if (allowed <= 0) { alert('Maximum 5 images per recipe.'); return; }

    const toUpload = Array.from(files).slice(0, allowed);
    const status = document.getElementById('uploadStatus');
    status.style.display = 'block';

    let hasError = false;
    for (let i = 0; i < toUpload.length; i++) {
        status.textContent = `Uploading ${i + 1}/${toUpload.length}...`;
        status.style.color = '';
        const formData = new FormData();
        formData.append('file', toUpload[i]);
        try {
            const uploadRes = await fetch(`/api/recipes/${recipeId}/images`, {
                method: 'POST',
                credentials: 'include',
                body: formData
            });
            if (!uploadRes.ok) {
                const errorBody = await uploadRes.text().catch(() => '');
                status.textContent = `Upload failed (HTTP ${uploadRes.status}): ${errorBody || 'Unknown error'}`;
                status.style.color = 'red';
                hasError = true;
            }
        } catch (e) {
            status.textContent = `Upload failed: ${e.message}`;
            status.style.color = 'red';
            hasError = true;
        }
    }

    if (!hasError) {
        status.textContent = 'Upload complete!';
        status.style.color = 'green';
        setTimeout(() => { status.style.display = 'none'; status.style.color = ''; }, 2000);
    }
    await loadRecipeImages(recipeId);
    await loadRecipes();
}

async function removeImage(recipeId, imageId) {
    try {
        await fetch(`/api/recipes/${recipeId}/images/${imageId}`, {
            method: 'DELETE',
            credentials: 'include'
        });
        await loadRecipeImages(recipeId);
        await loadRecipes();
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

// --- Toast ---
function showToast(message) {
    const toast = document.getElementById('toast');
    toast.textContent = message;
    toast.classList.add('show');
    setTimeout(() => { toast.classList.remove('show'); }, 3000);
}

// --- Utilities ---
function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

function openImageLightbox(src) {
    const overlay = document.createElement('div');
    overlay.className = 'image-lightbox-overlay';
    overlay.innerHTML = `<img src="${src}" class="image-lightbox-img" />`;
    overlay.addEventListener('click', () => overlay.remove());
    document.body.appendChild(overlay);
}

// --- Event Listeners ---
document.getElementById('backBtn').addEventListener('click', () => {
    window.location.href = '/tools.html';
});

document.getElementById('addRecipeBtn').addEventListener('click', openAddModal);
document.getElementById('modalClose').addEventListener('click', closeModal);
document.getElementById('cancelBtn').addEventListener('click', closeModal);
document.getElementById('recipeForm').addEventListener('submit', saveRecipe);

document.getElementById('recipeModal').addEventListener('click', (e) => {
    if (e.target.id === 'recipeModal') closeModal();
});

document.getElementById('confirmDeleteBtn').addEventListener('click', confirmDelete);
document.getElementById('cancelDeleteBtn').addEventListener('click', closeDeleteModal);
document.getElementById('deleteModal').addEventListener('click', (e) => {
    if (e.target.id === 'deleteModal') closeDeleteModal();
});

document.getElementById('searchInput').addEventListener('input', filterAndRender);
document.getElementById('categoryFilter').addEventListener('change', filterAndRender);

// --- Init ---
(async () => {
    const user = await checkAuth();
    if (!enforceToolPermission(user, 'recipes')) return;
    initDropZone();
    await loadRecipes();
    await loadCategories();
})();
