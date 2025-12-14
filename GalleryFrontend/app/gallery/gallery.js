// Debounce helper - waits until resizing stops
function debounce(fn, delay) {
    let timeout;
    return function(...args) {
        clearTimeout(timeout);
        timeout = setTimeout(() => fn.apply(this, args), delay);
    };
}

function justifyGallery(gallerySelector, targetHeight) {
    const gallery = document.querySelector(gallerySelector);
    const images = [...gallery.querySelectorAll('img')];
    
    Promise.all(images.map(img => {
        if (img.complete) return Promise.resolve();
        return new Promise(resolve => {
            img.onload = resolve;
            img.onerror = resolve;
        });
    })).then(() => {
        layoutGallery(gallery, images, targetHeight);
    });
}

function layoutGallery(gallery, images, targetHeight) {
    // Get computed styles to account for padding and gap
    const styles = getComputedStyle(gallery);
    const paddingLeft = parseFloat(styles.paddingLeft) || 0;
    const paddingRight = parseFloat(styles.paddingRight) || 0;
    const gap = parseFloat(styles.gap) || 0;
    
    // Actual usable width
    const containerWidth = gallery.clientWidth - paddingLeft - paddingRight;
    const containerHeight = gallery.clientHeight - (parseFloat(styles.paddingTop) || 0) - (parseFloat(styles.paddingBottom) || 0);
    const intededHeight = containerHeight / 3 - 20 ;

    
    console.log(`Container width: ${containerWidth}, padding: l${paddingLeft}:r${paddingRight}, gap: ${gap}, targetHeight: ${targetHeight}, intendedHeight: ${intededHeight}, containerHeight: ${containerHeight}`); 
    
    let row = [];
    let rowWidth = 0;
    
    images.forEach((img, index) => {
        const ratio = img.naturalWidth / img.naturalHeight;
        const imgWidth = targetHeight * ratio;
        
        // Gaps would be (row.length) gaps if we add this image
        const gapsWidth = row.length * gap;
        const potentialRowWidth = rowWidth + imgWidth + gapsWidth;
        
        if (potentialRowWidth > containerWidth && row.length > 0) {
            justifyRow(row, containerWidth, gap, false, targetHeight);
            row = [img];
            rowWidth = imgWidth;
        } else {
            row.push(img);
            rowWidth += imgWidth;
        }
        
        if (index === images.length - 1 && row.length > 0) {
            justifyRow(row, containerWidth, gap, true, targetHeight);
        }
    });
}

function justifyRow(images, containerWidth, gap, isLastRow, targetHeight) {
    const ratios = images.map(img => img.naturalWidth / img.naturalHeight);
    const totalGaps = (images.length - 1) * gap;
    const availableWidth = containerWidth - totalGaps;
    
    const totalRatio = ratios.reduce((sum, r) => sum + r, 0);
    let adjustedHeight = availableWidth / totalRatio;
    
    // Don't stretch last row too much
    const isJustified = !(isLastRow && adjustedHeight > targetHeight);
    if (!isJustified) {
        adjustedHeight = targetHeight;
    }
    
    // Calculate widths
    const widths = ratios.map(r => adjustedHeight * r);
    
    // Fix rounding: only add remainder if row is justified
    const flooredWidths = widths.map(w => Math.floor(w));
    const totalCalculatedWidth = flooredWidths.reduce((sum, w) => sum + w, 0);
    const remainder = isJustified ? Math.round(availableWidth - totalCalculatedWidth) : 0;
    
    images.forEach((img, i) => {
        const isLast = i === images.length - 1;
        const width = isLast ? (flooredWidths[i] + remainder -1) : flooredWidths[i];  //-1 to make sure it fits, just in case 
        
        img.style.height = `${Math.floor(adjustedHeight)}px`;
        img.style.width = `${width}px`;
    });
}

// Initialize on load
window.addEventListener('load', () => {
    justifyGallery('.gallery', 300);
});
window.addEventListener('load', debounce(() => {
    justifyGallery('.gallery', 300);
}, 150));

// Debounced resize
window.addEventListener('resize', debounce(() => {
    justifyGallery('.gallery', 300);
}, 150));