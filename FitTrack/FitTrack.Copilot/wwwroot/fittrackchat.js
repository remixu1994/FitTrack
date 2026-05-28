    // 滚动到聊天底部
window.fittrackChat = {
        scrollBottom: () => {
    const el = document.getElementById('chat-bottom');
    if (el) el.scrollIntoView({behavior: 'smooth', block: 'end'});
    }
};

    // 如需用原生 <input type="file"/> 读取 DataURL，可在这里实现；当前留空
window.fittrackFiles = {
    pickImageAsDataUrl: async () => null
};
