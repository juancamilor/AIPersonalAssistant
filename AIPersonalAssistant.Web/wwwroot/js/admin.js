let currentUserEmail = null;

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
            return `<tr>
                <td>${user.email}</td>
                <td>${roleBadge}</td>
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

(async () => {
    const user = await checkAuth();
    if (user) await loadUsers();
})();
