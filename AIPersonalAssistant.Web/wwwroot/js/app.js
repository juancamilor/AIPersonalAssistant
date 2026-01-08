if (!sessionStorage.getItem('isLoggedIn')) {
    window.location.href = '/login.html';
}

const loadTools = async () => {
    try {
        const response = await fetch('/api/tools');
        const tools = await response.json();
        
        const toolsList = document.getElementById('toolsList');
        toolsList.innerHTML = tools.map(tool => `<div class="tool-card">
            <div class="tool-icon">${tool.icon}</div>
            <div class="tool-name">${tool.name}</div>
            <div class="tool-description">${tool.description}</div>
        </div>`).join('');
    } catch (error) {
        console.error('Error loading tools:', error);
        document.getElementById('toolsList').innerHTML = '<p style="color: white;">Error loading tools. Please try again.</p>';
    }
};

document.getElementById('logoutBtn').addEventListener('click', () => {
    sessionStorage.clear();
    window.location.href = '/login.html';
});

loadTools();
