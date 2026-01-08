document.getElementById('loginForm').addEventListener('submit', (e) => {
    e.preventDefault();
    const username = document.getElementById('username').value;
    const password = document.getElementById('password').value;
    
    sessionStorage.setItem('isLoggedIn', 'true');
    sessionStorage.setItem('username', username);
    
    window.location.href = '/index.html';
});
