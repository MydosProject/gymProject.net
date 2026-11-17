// Admin "upload from computer" control for image link fields.
// The photo is shrunk in the browser (longest side 1600 px, WebP) before upload,
// so large phone photos stay small; the returned /media link goes into the field.
(() => {
    'use strict';

    const maxSide = 1600;
    const targetBytes = 900 * 1024;
    const qualities = [0.82, 0.7, 0.6];
    const allowedTypes = ['image/jpeg', 'image/png', 'image/webp'];

    const loadImage = async (file) => {
        if ('createImageBitmap' in window) {
            try {
                return await createImageBitmap(file, { imageOrientation: 'from-image' });
            } catch {
                // Fall back to an <img> element below.
            }
        }

        const url = URL.createObjectURL(file);
        try {
            const image = new Image();
            image.src = url;
            await image.decode();
            return image;
        } finally {
            URL.revokeObjectURL(url);
        }
    };

    const encode = (canvas, type, quality) =>
        new Promise((resolve) => canvas.toBlob(resolve, type, quality));

    const draw = (canvas, source, whiteBackground) => {
        const context = canvas.getContext('2d');
        context.clearRect(0, 0, canvas.width, canvas.height);
        if (whiteBackground) {
            context.fillStyle = '#ffffff';
            context.fillRect(0, 0, canvas.width, canvas.height);
        }
        context.drawImage(source, 0, 0, canvas.width, canvas.height);
    };

    // Returns a WebP (or JPEG where WebP cannot be written) under targetBytes, or null.
    const shrink = async (file) => {
        const source = await loadImage(file);
        try {
            let scale = Math.min(1, maxSide / Math.max(source.width, source.height));
            for (let attempt = 0; attempt < 4; attempt += 1) {
                const canvas = document.createElement('canvas');
                canvas.width = Math.max(1, Math.round(source.width * scale));
                canvas.height = Math.max(1, Math.round(source.height * scale));

                draw(canvas, source, false);
                let type = 'image/webp';
                for (const quality of qualities) {
                    let blob = await encode(canvas, type, quality);
                    if (blob && type === 'image/webp' && blob.type !== 'image/webp') {
                        // This browser cannot write WebP: use JPEG on a white background.
                        type = 'image/jpeg';
                        draw(canvas, source, true);
                        blob = await encode(canvas, type, quality);
                    }
                    if (blob && blob.size <= targetBytes) {
                        return blob;
                    }
                }
                scale *= 0.75;
            }
            return null;
        } finally {
            source.close?.();
        }
    };

    const upload = async (root, blob) => {
        const form = root.closest('form');
        const token = form?.querySelector('input[name="__RequestVerificationToken"]')?.value;
        const data = new FormData();
        const extension = blob.type === 'image/webp' ? 'webp' : blob.type === 'image/png' ? 'png' : 'jpg';
        data.append('file', blob, `photo.${extension}`);
        if (token) {
            data.append('__RequestVerificationToken', token);
        }

        const response = await fetch(root.dataset.uploadUrl, {
            method: 'POST',
            body: data,
            credentials: 'same-origin'
        });

        let payload = null;
        try {
            payload = await response.json();
        } catch {
            payload = null;
        }

        if (!response.ok || !payload?.url) {
            throw new Error(payload?.error
                || (response.status === 413 ? 'Fotoğraf çok büyük.' : 'Fotoğraf yüklenemedi. Lütfen tekrar deneyin.'));
        }

        return payload.url;
    };

    const init = (root) => {
        const target = document.getElementById(root.dataset.imageUploadFor);
        const input = root.querySelector('[data-image-upload-input]');
        const status = root.querySelector('[data-image-upload-status]');
        const preview = root.querySelector('[data-image-upload-preview]');
        if (!target || !input || !status || !preview) {
            return;
        }

        const defaultText = status.textContent.trim();
        const setStatus = (text, isError = false) => {
            status.textContent = text;
            status.classList.toggle('is-error', isError);
        };

        const showPreview = () => {
            const url = target.value.trim();
            preview.hidden = !url;
            if (url) {
                preview.src = url;
            } else {
                preview.removeAttribute('src');
            }
        };

        preview.addEventListener('error', () => { preview.hidden = true; });
        target.addEventListener('change', showPreview);
        target.addEventListener('input', showPreview);
        showPreview();

        input.addEventListener('change', async () => {
            const file = input.files?.[0];
            if (!file) {
                return;
            }

            if (!allowedTypes.includes(file.type)) {
                setStatus('Yalnız JPEG, PNG veya WebP fotoğraf seçin.', true);
                input.value = '';
                return;
            }

            const buttons = [...(root.closest('form')?.querySelectorAll('button[type="submit"], button:not([type])') ?? [])];
            buttons.forEach((button) => { button.disabled = true; });
            setStatus('Fotoğraf küçültülüyor…');

            try {
                let blob = null;
                try {
                    blob = await shrink(file);
                } catch {
                    blob = null;
                }

                setStatus('Yükleniyor…');
                const url = await upload(root, blob ?? file);
                target.value = url;
                target.dispatchEvent(new Event('change', { bubbles: true }));
                const sizeKb = Math.max(1, Math.round((blob ?? file).size / 1024));
                setStatus(`Fotoğraf yüklendi (${sizeKb} KB). Değişikliği kaydetmeyi unutmayın.`);
            } catch (error) {
                setStatus(error?.message || defaultText, true);
            } finally {
                input.value = '';
                buttons.forEach((button) => { button.disabled = false; });
            }
        });
    };

    document.querySelectorAll('[data-image-upload]').forEach(init);
})();
