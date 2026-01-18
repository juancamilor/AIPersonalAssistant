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
        document.getElementById('userEmail').textContent = user.email;
        return user;
    } catch (error) {
        console.error('Auth check failed:', error);
        window.location.href = '/login.html';
        return null;
    }
};

const loadTools = async () => {
    try {
        const response = await fetch('/api/tools', {
            credentials: 'include'
        });
        
        if (!response.ok) {
            if (response.status === 401) {
                window.location.href = '/login.html';
                return;
            }
            throw new Error('Failed to load tools');
        }
        
        const tools = await response.json();
        
        const toolsList = document.getElementById('toolsList');
        toolsList.innerHTML = tools.map(tool => `<div class="tool-card" data-tool-id="${tool.id}" data-tool-name="${tool.name}">
            <div class="tool-icon">${tool.icon}</div>
            <div class="tool-name">${tool.name}</div>
            <div class="tool-description">${tool.description}</div>
        </div>`).join('');
        
        const cards = document.querySelectorAll('.tool-card');
        cards.forEach(card => {
            card.addEventListener('click', handleToolClick);
        });
    } catch (error) {
        console.error('Error loading tools:', error);
        document.getElementById('toolsList').innerHTML = '<p style="color: white;">Error loading tools. Please try again.</p>';
    }
};

const handleToolClick = (event) => {
    const card = event.currentTarget;
    const toolName = card.dataset.toolName;
    
    if (toolName === 'Rate Exchange') {
        window.location.href = '/rate-exchange.html';
    }
};

document.getElementById('logoutBtn').addEventListener('click', () => {
    window.location.href = '/api/auth/logout';
});

(async () => {
    await checkAuth();
    await loadTools();
})();

