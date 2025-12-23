window.fileUpload = {
    dotNetHelper: null,

    initializeFileInput: function (inputId, dotNetHelper) {
        this.dotNetHelper = dotNetHelper;
        const input = document.getElementById(inputId);

        if (input) {
            input.addEventListener('change', async (e) => {
                const file = e.target.files[0];
                if (file) {
                    const reader = new FileReader();
                    reader.onload = (event) => {
                        this.dotNetHelper.invokeMethodAsync('OnFileSelected', event.target.result);
                    };
                    reader.readAsDataURL(file);
                }
            });
        }
    },

    triggerFileInput: function (inputId) {
        const input = document.getElementById(inputId);
        if (input) {
            input.click();
        }
    }
};
