window.openPdf = function (base64Data) {
    console.log("openPdf called, length:", base64Data.length);
    const byteCharacters = atob(base64Data);
    const byteNumbers = new Array(byteCharacters.length);
    for (let i = 0; i < byteCharacters.length; i++) {
        byteNumbers[i] = byteCharacters.charCodeAt(i);
    }
    const byteArray = new Uint8Array(byteNumbers);
    const blob = new Blob([byteArray], { type: 'application/pdf' });
    const url = URL.createObjectURL(blob);
    window.open(url, "_blank");
};

window.downloadFile = function (fileName, base64Data) {
    const link = document.createElement('a');
    link.download = fileName;
    link.href = "data:application/pdf;base64," + base64Data;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
};