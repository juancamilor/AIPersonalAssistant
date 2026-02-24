let currentUserEmail = null;
let allTools = [];
let editingPermEmail = null;

const loadTools = async () => {
    try {
        const resp = await fetch('/api/admin/tools', { credentials: 'include' });
        if (resp.ok) allTools = await resp.json();
    } catch (error) {
        console.error('Error loading tools:', error);
    }
};

const checkAuth = async () => {
    try {
        const response = await fetch('/api/auth/user', {
            credentials: 'include'
        });

        if (!response.ok) {
            window.location.href = '/login.html';
            return null;
        }

        const user = await response.json();
        if (!user.isAdmin) {
            window.location.href = '/tools.html';
            return null;
        }

        currentUserEmail = user.email;
        return user;
    } catch (error) {
        console.error('Auth check failed:', error);
        window.location.href = '/login.html';
        return null;
    }
};

const loadUsers = async () => {
    try {
        const response = await fetch('/api/admin/users', {
            credentials: 'include'
        });

        if (!response.ok) {
            if (response.status === 401) {
                window.location.href = '/login.html';
                return;
            }
            throw new Error('Failed to load users');
        }

        const users = await response.json();
        const tbody = document.getElementById('usersBody');
        const noUsers = document.getElementById('noUsers');
        const table = document.getElementById('usersTable');

        if (!users.length) {
            table.style.display = 'none';
            noUsers.style.display = '';
            return;
        }

        table.style.display = '';
        noUsers.style.display = 'none';

        tbody.innerHTML = users.map(user => {
            const isSelf = user.email === currentUserEmail;
            const roleBadge = user.role === 'Admin'
                ? '<span class="role-badge admin">Admin</span>'
                : '<span class="role-badge user">User</span>';
            const removeBtn = isSelf
                ? '<button class="remove-btn" disabled title="Cannot remove yourself">Remove</button>'
                : `<button class="remove-btn" onclick="removeUser('${user.email}')">Remove</button>`;
            const perms = user.permissions || [];
            const permBadge = perms.includes('*')
                ? '<span class="permissions-badge all">All Tools</span>'
                : `<span class="permissions-badge partial">${perms.length} tool${perms.length !== 1 ? 's' : ''}</span>`;
            const editPermBtn = `<button class="edit-perm-btn" onclick="openPermissionsModal('${user.email}')">Edit</button>`;
            return `<tr>
                <td>${user.email}</td>
                <td>${roleBadge}</td>
                <td>${permBadge} ${editPermBtn}</td>
                <td>${removeBtn}</td>
            </tr>`;
        }).join('');
    } catch (error) {
        console.error('Error loading users:', error);
    }
};

const addUser = async (email) => {
    try {
        const response = await fetch('/api/admin/users', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            credentials: 'include',
            body: JSON.stringify({ email })
        });

        if (!response.ok) {
            const err = await response.text();
            alert('Failed to add user: ' + err);
            return;
        }

        document.getElementById('emailInput').value = '';
        await loadUsers();
    } catch (error) {
        console.error('Error adding user:', error);
        alert('Error adding user.');
    }
};

const removeUser = async (email) => {
    if (!confirm(`Remove user "${email}"?`)) return;

    try {
        const response = await fetch(`/api/admin/users/${encodeURIComponent(email)}`, {
            method: 'DELETE',
            credentials: 'include'
        });

        if (!response.ok) {
            const err = await response.text();
            alert('Failed to remove user: ' + err);
            return;
        }

        await loadUsers();
    } catch (error) {
        console.error('Error removing user:', error);
        alert('Error removing user.');
    }
};

document.getElementById('addUserForm').addEventListener('submit', (e) => {
    e.preventDefault();
    const email = document.getElementById('emailInput').value.trim();
    if (email) addUser(email);
});

const openPermissionsModal = async (email) => {
    editingPermEmail = email;
    document.getElementById('permModalEmail').textContent = email;

    const checkboxesDiv = document.getElementById('toolCheckboxes');
    checkboxesDiv.innerHTML = allTools.map(tool => `
        <label class="perm-option">
            <input type="checkbox" value="${tool}">
            <span>${tool}</span>
        </label>
    `).join('');

    try {
        const resp = await fetch(`/api/admin/users/${encodeURIComponent(email)}/permissions`, { credentials: 'include' });
        if (!resp.ok) throw new Error('Failed to load permissions');
        const perms = await resp.json();

        const permAll = document.getElementById('permAll');
        permAll.checked = perms.includes('*');

        const toolBoxes = checkboxesDiv.querySelectorAll('input[type="checkbox"]');
        toolBoxes.forEach(cb => {
            cb.checked = perms.includes('*') || perms.includes(cb.value);
            cb.disabled = perms.includes('*');
        });
    } catch (error) {
        console.error('Error loading permissions:', error);
        alert('Error loading permissions.');
        return;
    }

    document.getElementById('permissionsModal').style.display = 'flex';
};

const closePermissionsModal = () => {
    document.getElementById('permissionsModal').style.display = 'none';
    editingPermEmail = null;
};

const toggleAllPermissions = (checkbox) => {
    const toolBoxes = document.getElementById('toolCheckboxes').querySelectorAll('input[type="checkbox"]');
    toolBoxes.forEach(cb => {
        cb.disabled = checkbox.checked;
        if (checkbox.checked) cb.checked = true;
    });
};

const savePermissions = async () => {
    if (!editingPermEmail) return;

    const permAll = document.getElementById('permAll');
    let permissions;

    if (permAll.checked) {
        permissions = ['*'];
    } else {
        const toolBoxes = document.getElementById('toolCheckboxes').querySelectorAll('input[type="checkbox"]');
        permissions = Array.from(toolBoxes).filter(cb => cb.checked).map(cb => cb.value);
    }

    try {
        const resp = await fetch(`/api/admin/users/${encodeURIComponent(editingPermEmail)}/permissions`, {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json' },
            credentials: 'include',
            body: JSON.stringify(permissions)
        });

        if (!resp.ok) {
            const err = await resp.text();
            alert('Failed to save permissions: ' + err);
            return;
        }

        closePermissionsModal();
        await loadUsers();
    } catch (error) {
        console.error('Error saving permissions:', error);
        alert('Error saving permissions.');
    }
};

(async () => {
    const user = await checkAuth();
    if (user) {
        await loadTools();
        await loadUsers();
    }
})();
