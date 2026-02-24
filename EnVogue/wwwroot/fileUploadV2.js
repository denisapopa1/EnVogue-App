window.fileUpload = {
    triggerFileInput: function (id) {
        document.getElementById(id).click();
    },
    initializeFileInput: function (element, dotNetReference) {
        element.onchange = function () {
            const file = element.files[0];
            if (file) {
                // Verificăm dacă e imagine
                if (!file.type.startsWith('image/')) return;

                const img = new Image();
                img.onload = function () {
                    const canvas = document.createElement('canvas');
                    const MAX_WIDTH = 800; // O facem foarte mică pentru test
                    const scaleSize = MAX_WIDTH / img.width;
                    canvas.width = MAX_WIDTH;
                    canvas.height = img.height * scaleSize;

                    const ctx = canvas.getContext('2d');
                    ctx.drawImage(img, 0, 0, canvas.width, canvas.height);

                    // Calitate mică (0.5) pentru a fi siguri că nu blochează nimic
                    const base64Data = canvas.toDataURL('image/jpeg', 0.5);

                    dotNetReference.invokeMethodAsync('OnFileSelected', base64Data);

                    URL.revokeObjectURL(img.src);
                    element.value = '';
                };
                img.src = URL.createObjectURL(file);
            }
        };
    }
};