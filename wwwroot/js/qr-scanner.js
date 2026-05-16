const QRScanner = {
    video: null,
    canvas: null,
    ctx: null,
    stream: null,
    scanning: false,
    onResult: null,

    async init(videoEl, canvasEl, onResult) {
        this.video = videoEl;
        this.canvas = canvasEl;
        this.ctx = canvasEl.getContext('2d');
        this.onResult = onResult;
    },

    async start() {
        try {
            this.stream =
                await navigator.mediaDevices
                    .getUserMedia({
                        video: {
                            facingMode: 'environment',
                            width: { ideal: 1280 },
                            height: { ideal: 720 }
                        }
                    });

            this.video.srcObject = this.stream;
            await this.video.play();
            this.scanning = true;
            this.scan();
            return true;
        } catch (err) {
            console.error('Camera error:', err);
            return false;
        }
    },

    stop() {
        this.scanning = false;
        if (this.stream) {
            this.stream.getTracks()
                .forEach(t => t.stop());
            this.stream = null;
        }
    },

    scan() {
        if (!this.scanning) return;

        if (this.video.readyState ===
            this.video.HAVE_ENOUGH_DATA) {
            const { videoWidth: w,
                videoHeight: h } = this.video;
            this.canvas.width = w;
            this.canvas.height = h;
            this.ctx.drawImage(this.video, 0, 0, w, h);

            const imageData = this.ctx.getImageData(
                0, 0, w, h);

            // jsQR library
            if (typeof jsQR !== 'undefined') {
                const code = jsQR(
                    imageData.data,
                    imageData.width,
                    imageData.height,
                    {
                        inversionAttempts:
                            'dontInvert'
                    });

                if (code) {
                    this.drawBoundary(code.location);
                    if (this.onResult)
                        this.onResult(code.data);
                    return;
                }
            }
        }

        requestAnimationFrame(() => this.scan());
    },

    drawBoundary(loc) {
        this.ctx.beginPath();
        this.ctx.moveTo(
            loc.topLeftCorner.x,
            loc.topLeftCorner.y);
        this.ctx.lineTo(
            loc.topRightCorner.x,
            loc.topRightCorner.y);
        this.ctx.lineTo(
            loc.bottomRightCorner.x,
            loc.bottomRightCorner.y);
        this.ctx.lineTo(
            loc.bottomLeftCorner.x,
            loc.bottomLeftCorner.y);
        this.ctx.closePath();
        this.ctx.lineWidth = 4;
        this.ctx.strokeStyle = '#28a745';
        this.ctx.stroke();
    }
};