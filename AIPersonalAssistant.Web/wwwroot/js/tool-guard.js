const checkToolPermission = (user, toolId) => {
    if (!user || !user.permissions) return false;
    if (user.permissions.includes('*')) return true;
    return user.permissions.includes(toolId);
};

const enforceToolPermission = (user, toolId) => {
    if (!checkToolPermission(user, toolId)) {
        window.location.href = '/tools.html?error=noperm&tool=' + encodeURIComponent(toolId);
        return false;
    }
    return true;
};
