if (!sessionStorage.getItem('isLoggedIn')) {
    window.location.href = '/login.html';
}

const loadTools = async () => {
    try {
        const response = await fetch('/api/tools');
        const tools = await response.json();
        
        console.log('Loaded tools:', tools); // Debug log
        
        const toolsList = document.getElementById('toolsList');
        toolsList.innerHTML = tools.map(tool => `<div class="tool-card" data-tool-id="${tool.id}" data-tool-name="${tool.name}">
            <div class="tool-icon">${tool.icon}</div>
            <div class="tool-name">${tool.name}</div>
            <div class="tool-description">${tool.description}</div>
        </div>`).join('');
        
        // Add click handlers to tool cards
        const cards = document.querySelectorAll('.tool-card');
        console.log('Found tool cards:', cards.length); // Debug log
        
        cards.forEach(card => {
            console.log('Adding listener to card:', card.dataset.toolName); // Debug log
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
    
    console.log('Tool clicked:', toolName); // Debug log
    
    // Navigate based on tool name
    if (toolName === 'Rate Exchange') {
        window.location.href = '/rate-exchange.html';
    }
    // Add more navigation cases here for other tools as they are implemented
};

document.getElementById('logoutBtn').addEventListener('click', () => {
    sessionStorage.clear();
    window.location.href = '/login.html';
});

loadTools();
