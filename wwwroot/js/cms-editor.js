/* ===== NAU CMS inline editor (admin only) =====
   Lets a logged-in Admin edit page text directly on the page and swap
   images in place, then save the result back to the Pages table via
   CmsController (SaveHomeContent / SaveAboutHistoryCaaContent /
   SaveAboutHistoryCatiContent / UploadImage).

   initCmsEditor(ids) creates one independent editor instance bound to a
   specific set of element ids, so a single page can host more than one
   editable region (e.g. the About page's "History of CAA" AND "History of
   CATI" sections) without them interfering with each other. Calling it
   with no arguments falls back to the original default ids, so pages that
   only ever had one region (the Homepage) keep working unchanged.
*/
function initCmsEditor(ids) {
    "use strict";
    ids = ids || {};

    var region   = document.getElementById(ids.region || "cms-editable-region");
    var editBtn  = document.getElementById(ids.editBtn || "cmsEditBtn");
    var saveBtn  = document.getElementById(ids.saveBtn || "cmsSaveBtn");
    var cancelBtn= document.getElementById(ids.cancelBtn || "cmsCancelBtn");
    var status   = document.getElementById(ids.status || "cmsStatus");
    var fileInput= document.getElementById(ids.fileInput || "cmsImageInput");
    if (!region || !editBtn) return null;

    // Which endpoint to POST the saved HTML to. Defaults to the homepage's
    // save action (original behaviour); other pages opt into a different
    // endpoint by setting data-save-url="..." on the editable region
    // element (e.g. the About page's "History of CAA" / "History of CATI"
    // sections).
    var saveUrl = region.getAttribute("data-save-url") || "/Cms/SaveHomeContent";

    var activeImg = null; // the <img> currently being replaced
    var activeBgEl = null; // the background-image element currently being replaced
    var activeVideoEl = null; // the <video> currently being replaced
    var activeHeroVideoSlide = null; // the hero slide getting its first-ever video

    // A second hidden file input, dedicated to video uploads (kept separate
    // from the image one so we can restrict the "accept" filter correctly).
    var videoInput = document.getElementById(ids.videoInput || "cmsVideoInput");

    var antiForgeryFormId = ids.antiForgeryForm || "cmsAntiForgeryForm";

    function antiForgeryToken() {
        var form = document.getElementById(antiForgeryFormId);
        if (form) {
            var tokenInput = form.querySelector('input[name="__RequestVerificationToken"]');
            if (tokenInput && tokenInput.value) return tokenInput.value;
        }
        var globalTokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
        return globalTokenInput ? globalTokenInput.value : "";
    }

    function setStatus(text, isError) {
        status.textContent = text || "";
        status.style.color = isError ? "#ff8a80" : "#fff";
    }

    // ===== IMAGE UPLOAD & MODAL SYSTEM =====
    function uploadImageFile(file) {
        if (!file) return Promise.reject(new Error("No file selected"));
        setStatus("Uploading image...");
        var fd = new FormData();
        fd.append("file", file);
        fd.append("__RequestVerificationToken", antiForgeryToken());

        return fetch("/Cms/UploadImage", { method: "POST", body: fd })
            .then(function (r) { return r.json(); })
            .then(function (data) {
                if (data.success) {
                    setStatus("Image uploaded! Click Save Changes to publish.");
                    return data.url;
                } else {
                    setStatus(data.message || "Upload failed.", true);
                    throw new Error(data.message || "Upload failed.");
                }
            })
            .catch(function (err) {
                setStatus(err.message || "Upload failed. Check your connection.", true);
                throw err;
            });
    }

    function openImagePickerModal(opts) {
        opts = opts || {};
        var currentUrl = opts.currentUrl || "";
        var currentPos = opts.currentPosition || "50% 50%";
        var title = opts.title || "Choose Image";
        var defaultTab = opts.defaultTab || "upload";
        var targetElement = opts.targetElement;

        var existing = document.getElementById("cmsImageModalBackdrop");
        if (existing) existing.remove();

        var backdrop = document.createElement("div");
        backdrop.id = "cmsImageModalBackdrop";
        backdrop.className = "cms-image-modal-backdrop";

        // Calculate exact proportional dimensions of target placeholder/element
        var maxBoxW = 460;
        var maxBoxH = 260;
        var targetW = 400;
        var targetH = 300;

        if (targetElement) {
            var rect = targetElement.getBoundingClientRect();
            if (rect.width > 0 && rect.height > 0) {
                targetW = rect.width;
                targetH = rect.height;
            }
        }

        var scale = Math.min(maxBoxW / targetW, maxBoxH / targetH);
        var previewW = Math.round(targetW * scale);
        var previewH = Math.round(targetH * scale);
        var previewSizeStyle = "width: " + previewW + "px; height: " + previewH + "px;";

        var activeUrl = currentUrl;
        var activePos = currentPos;

        backdrop.innerHTML = [
            '<div class="cms-image-modal">',
            '  <div class="cms-image-modal-head">',
            '    <h3><i class="fas fa-image"></i> ' + title + '</h3>',
            '    <button type="button" class="cms-image-modal-close" title="Close">&times;</button>',
            '  </div>',
            '  <div class="cms-image-modal-tabs">',
            '    <button type="button" class="cms-image-modal-tab ' + (defaultTab === "upload" ? "active" : "") + '" data-tab="upload"><i class="fas fa-upload"></i> Upload File</button>',
            '    <button type="button" class="cms-image-modal-tab ' + (defaultTab === "url" ? "active" : "") + '" data-tab="url"><i class="fas fa-link"></i> Web URL</button>',
            '  </div>',
            '  <div class="cms-image-modal-body">',
            '    <!-- TAB 1: UPLOAD -->',
            '    <div class="cms-tab-content" data-tab-content="upload" style="display:' + (defaultTab === "upload" ? "block" : "none") + ';">',
            '      <div class="cms-dropzone" id="cmsModalDropzone">',
            '        <i class="fas fa-cloud-arrow-up"></i>',
            '        <div class="cms-dropzone-title">Drag & drop image here</div>',
            '        <div class="cms-dropzone-sub">or click to browse from computer (PNG, JPG, WebP)</div>',
            '      </div>',
            '      <input type="file" id="cmsModalFileInput" accept="image/*" style="display:none;">',
            '      <div class="cms-preview-section" id="cmsUploadPreviewSection" style="display:' + (activeUrl ? "block" : "none") + ';">',
            '        <div class="cms-preview-label"><span>Placeholder Preview (' + Math.round(targetW) + ' &times; ' + Math.round(targetH) + 'px):</span><span style="font-size:11px;color:#1e8e3e;font-weight:600;"><i class="fas fa-arrows-up-down-left-right"></i> Drag image to move position</span></div>',
            '        <div class="cms-preview-container">',
            '          <div class="cms-preview-wrapper" id="cmsUploadPreviewWrap" style="' + previewSizeStyle + '">',
            '            <img id="cmsUploadPreviewImg" src="' + (activeUrl || '') + '" alt="Preview" style="object-position:' + activePos + ';">',
            '            <div class="cms-drag-hint"><i class="fas fa-arrows-up-down-left-right"></i> Drag to move position</div>',
            '          </div>',
            '        </div>',
            '      </div>',
            '    </div>',
            '    <!-- TAB 2: URL -->',
            '    <div class="cms-tab-content" data-tab-content="url" style="display:' + (defaultTab === "url" ? "block" : "none") + ';">',
            '      <div class="cms-url-group">',
            '        <label for="cmsModalUrlInput">Image Web Address (URL):</label>',
            '        <input type="text" id="cmsModalUrlInput" class="cms-url-input" placeholder="https://images.unsplash.com/... or /images/photo.jpg" value="' + (currentUrl && !currentUrl.startsWith('data:') ? currentUrl : '') + '">',
            '      </div>',
            '      <div class="cms-preview-section" id="cmsUrlPreviewSection" style="display:' + (activeUrl ? "block" : "none") + ';">',
            '        <div class="cms-preview-label"><span>Placeholder Preview (' + Math.round(targetW) + ' &times; ' + Math.round(targetH) + 'px):</span><span style="font-size:11px;color:#1e8e3e;font-weight:600;"><i class="fas fa-arrows-up-down-left-right"></i> Drag image to move position</span></div>',
            '        <div class="cms-preview-container">',
            '          <div class="cms-preview-wrapper" id="cmsUrlPreviewWrap" style="' + previewSizeStyle + '">',
            '            <img id="cmsUrlPreviewImg" src="' + (activeUrl || '') + '" alt="Preview" style="object-position:' + activePos + ';">',
            '            <div class="cms-drag-hint"><i class="fas fa-arrows-up-down-left-right"></i> Drag to move position</div>',
            '          </div>',
            '        </div>',
            '      </div>',
            '    </div>',
            '    <!-- FOOTER ACTIONS -->',
            '    <div class="cms-modal-actions">',
            '      <div>',
            '        ' + (opts.allowRemove ? '<button type="button" class="cms-modal-btn cms-modal-btn-danger" id="cmsModalRemoveBtn"><i class="fas fa-trash-can"></i> Remove Image</button>' : ''),
            '      </div>',
            '      <div class="cms-modal-actions-right">',
            '        <button type="button" class="cms-modal-btn cms-modal-btn-cancel" id="cmsModalCancelBtn">Cancel</button>',
            '        <button type="button" class="cms-modal-btn cms-modal-btn-primary" id="cmsModalApplyBtn"><i class="fas fa-check"></i> Apply Image</button>',
            '      </div>',
            '    </div>',
            '  </div>',
            '</div>'
        ].join("\n");

        document.body.appendChild(backdrop);

        function closeModal() {
            backdrop.remove();
        }

        backdrop.querySelector(".cms-image-modal-close").addEventListener("click", closeModal);
        backdrop.addEventListener("click", function (e) {
            if (e.target === backdrop) closeModal();
        });
        backdrop.querySelector("#cmsModalCancelBtn").addEventListener("click", closeModal);

        // Remove button handler
        var removeBtn = backdrop.querySelector("#cmsModalRemoveBtn");
        if (removeBtn) {
            removeBtn.addEventListener("click", function () {
                if (confirm("Remove this image and restore the placeholder?")) {
                    if (opts.onRemove) opts.onRemove();
                    closeModal();
                }
            });
        }

        // Tabs
        var tabBtns = backdrop.querySelectorAll(".cms-image-modal-tab");
        var tabContents = backdrop.querySelectorAll(".cms-tab-content");
        tabBtns.forEach(function (btn) {
            btn.addEventListener("click", function () {
                var tab = btn.getAttribute("data-tab");
                tabBtns.forEach(function (b) { b.classList.toggle("active", b === btn); });
                tabContents.forEach(function (c) {
                    c.style.display = c.getAttribute("data-tab-content") === tab ? "block" : "none";
                });
            });
        });

        // Draggable Image Pan Functionality
        function setupDraggablePan(wrapper, img) {
            if (!wrapper || !img) return;

            var posX = 50, posY = 50;
            if (activePos) {
                var parts = activePos.replace(/px/g, "").replace(/%/g, "").split(" ");
                if (parts.length >= 2) {
                    posX = parseFloat(parts[0]) || 50;
                    posY = parseFloat(parts[1]) || 50;
                }
            }

            img.style.objectPosition = posX + "% " + posY + "%";
            img.style.cursor = "grab";
            img.style.userSelect = "none";
            img.setAttribute("draggable", "false");

            var isDragging = false;
            var startMouseX = 0, startMouseY = 0;
            var startPosX = posX, startPosY = posY;

            wrapper.onpointerdown = function (e) {
                isDragging = true;
                img.style.cursor = "grabbing";
                startMouseX = e.clientX;
                startMouseY = e.clientY;
                startPosX = posX;
                startPosY = posY;
                if (wrapper.setPointerCapture) {
                    try { wrapper.setPointerCapture(e.pointerId); } catch (err) {}
                }
                e.preventDefault();
            };

            wrapper.onpointermove = function (e) {
                if (!isDragging) return;
                var rect = wrapper.getBoundingClientRect();
                var cw = rect.width || 300;
                var ch = rect.height || 200;
                var dx = e.clientX - startMouseX;
                var dy = e.clientY - startMouseY;

                var newX = startPosX - (dx / cw) * 100;
                var newY = startPosY - (dy / ch) * 100;

                posX = Math.max(0, Math.min(100, newX));
                posY = Math.max(0, Math.min(100, newY));

                activePos = Math.round(posX) + "% " + Math.round(posY) + "%";
                img.style.objectPosition = activePos;
            };

            wrapper.onpointerup = wrapper.onpointercancel = function () {
                if (isDragging) {
                    isDragging = false;
                    img.style.cursor = "grab";
                }
            };
        }

        var uploadWrap = backdrop.querySelector("#cmsUploadPreviewWrap");
        var uploadImg = backdrop.querySelector("#cmsUploadPreviewImg");
        var uploadSection = backdrop.querySelector("#cmsUploadPreviewSection");
        setupDraggablePan(uploadWrap, uploadImg);

        var urlWrap = backdrop.querySelector("#cmsUrlPreviewWrap");
        var urlImg = backdrop.querySelector("#cmsUrlPreviewImg");
        var urlSection = backdrop.querySelector("#cmsUrlPreviewSection");
        setupDraggablePan(urlWrap, urlImg);

        // Upload Tab
        var dropzone = backdrop.querySelector("#cmsModalDropzone");
        var modalFileInput = backdrop.querySelector("#cmsModalFileInput");

        dropzone.addEventListener("click", function () {
            modalFileInput.click();
        });

        function handleModalFile(file) {
            if (!file) return;
            dropzone.innerHTML = '<i class="fas fa-spinner fa-spin"></i><div class="cms-dropzone-title">Uploading image...</div>';
            uploadImageFile(file)
                .then(function (url) {
                    activeUrl = url;
                    dropzone.innerHTML = '<i class="fas fa-check-circle" style="color:#1e8e3e;"></i><div class="cms-dropzone-title">Image ready!</div><div class="cms-dropzone-sub">Click above to pick another, or drag preview below to position</div>';
                    uploadImg.src = url;
                    uploadSection.style.display = "block";
                    setupDraggablePan(uploadWrap, uploadImg);
                })
                .catch(function () {
                    dropzone.innerHTML = '<i class="fas fa-cloud-arrow-up"></i><div class="cms-dropzone-title">Upload failed. Click to try again</div>';
                });
        }

        modalFileInput.addEventListener("change", function () {
            handleModalFile(modalFileInput.files[0]);
        });

        dropzone.addEventListener("dragover", function (e) {
            e.preventDefault();
            e.stopPropagation();
            dropzone.classList.add("dragover");
        });
        dropzone.addEventListener("dragleave", function (e) {
            e.preventDefault();
            e.stopPropagation();
            dropzone.classList.remove("dragover");
        });
        dropzone.addEventListener("drop", function (e) {
            e.preventDefault();
            e.stopPropagation();
            dropzone.classList.remove("dragover");
            if (e.dataTransfer.files && e.dataTransfer.files.length > 0) {
                handleModalFile(e.dataTransfer.files[0]);
            }
        });

        // URL Tab
        var urlInput = backdrop.querySelector("#cmsModalUrlInput");
        function updateUrlPreview() {
            var val = urlInput.value.trim();
            if (val) {
                activeUrl = val;
                urlImg.src = val;
                urlSection.style.display = "block";
                setupDraggablePan(urlWrap, urlImg);
            } else {
                urlSection.style.display = "none";
            }
        }
        urlInput.addEventListener("input", updateUrlPreview);

        // Apply Button
        var applyBtn = backdrop.querySelector("#cmsModalApplyBtn");
        applyBtn.addEventListener("click", function () {
            var finalUrl = activeUrl || (urlInput ? urlInput.value.trim() : "");
            if (!finalUrl) {
                alert("Please upload a file or enter an image URL.");
                return;
            }
            if (opts.onSelect) opts.onSelect(finalUrl, activePos);
            closeModal();
        });
    }

    // Direct Drag and Drop Handler
    function setupDirectDragAndDrop(el, onImageReceived) {
        if (!el || el._cmsDragConfigured) return;
        el._cmsDragConfigured = true;
        el.classList.add("cms-drag-target");

        el.addEventListener("dragover", function (e) {
            if (!document.body.classList.contains("cms-editing")) return;
            e.preventDefault();
            e.stopPropagation();
            el.classList.add("cms-drag-active");
        });
        el.addEventListener("dragleave", function (e) {
            if (!document.body.classList.contains("cms-editing")) return;
            e.preventDefault();
            e.stopPropagation();
            el.classList.remove("cms-drag-active");
        });
        el.addEventListener("drop", function (e) {
            if (!document.body.classList.contains("cms-editing")) return;
            e.preventDefault();
            e.stopPropagation();
            el.classList.remove("cms-drag-active");

            var files = e.dataTransfer.files;
            if (files && files.length > 0) {
                var file = files[0];
                if (file.type && file.type.indexOf("image") >= 0) {
                    uploadImageFile(file).then(function (url) {
                        onImageReceived(url, "50% 50%");
                    });
                    return;
                }
            }

            var text = e.dataTransfer.getData("text/uri-list") || e.dataTransfer.getData("text/plain");
            if (text && (text.startsWith("http://") || text.startsWith("https://") || text.startsWith("/"))) {
                onImageReceived(text.trim(), "50% 50%");
            }
        });
    }

    // If the site logo (".logo") doesn't have an actual <img> in it yet,
    // give it a placeholder image slot so the admin can upload/replace the logo.
    function ensureLogoImage() {
        var logo = region.querySelector(".logo");
        if (!logo) return;
        if (logo.querySelector("img")) return;

        var img = document.createElement("img");
        img.src = "/images/logo-placeholder.svg";
        img.alt = "University Logo";
        img.style.width = "60px";
        img.style.height = "60px";
        img.style.objectFit = "contain";
        logo.insertBefore(img, logo.firstChild);
    }

    function enableLiveImagePan(imgElement) {
        if (!imgElement || imgElement._cmsLivePanEnabled) return;
        imgElement._cmsLivePanEnabled = true;

        var isDragging = false;
        var startMouseX = 0, startMouseY = 0;
        var startPosX = 50, startPosY = 50;

        imgElement.addEventListener("pointerdown", function (e) {
            if (!document.body.classList.contains("cms-editing")) return;
            if (e.target.closest(".cms-action-btn, .cms-img-btn-group, .cms-placeholder-change-btn")) return;

            var curPos = imgElement.style.objectPosition || "50% 50%";
            var parts = curPos.replace(/px/g, "").replace(/%/g, "").split(" ");
            if (parts.length >= 2) {
                startPosX = parseFloat(parts[0]) || 50;
                startPosY = parseFloat(parts[1]) || 50;
            }

            isDragging = true;
            imgElement.style.cursor = "grabbing";
            startMouseX = e.clientX;
            startMouseY = e.clientY;
            if (imgElement.setPointerCapture) {
                try { imgElement.setPointerCapture(e.pointerId); } catch (err) {}
            }
            e.preventDefault();
        });

        imgElement.addEventListener("pointermove", function (e) {
            if (!isDragging) return;
            var rect = imgElement.getBoundingClientRect();
            var cw = rect.width || 300;
            var ch = rect.height || 200;
            var dx = e.clientX - startMouseX;
            var dy = e.clientY - startMouseY;

            var newX = startPosX - (dx / cw) * 100;
            var newY = startPosY - (dy / ch) * 100;

            var posX = Math.max(0, Math.min(100, newX));
            var posY = Math.max(0, Math.min(100, newY));

            imgElement.style.objectPosition = Math.round(posX) + "% " + Math.round(posY) + "%";
        });

        function endDrag() {
            if (isDragging) {
                isDragging = false;
                imgElement.style.cursor = "";
                setStatus("Image position updated. Click Save Changes to publish.");
            }
        }

        imgElement.addEventListener("pointerup", endDrag);
        imgElement.addEventListener("pointercancel", endDrag);
    }

    // Wrap every <img> in the region with change/remove action buttons and drag & drop.
    function wrapImages() {
        var imgs = region.querySelectorAll("img:not(.cms-wrapped)");
        imgs.forEach(function (img) {
            if (img.closest(".announcement, nav.navbar, .news-events, .news-events-inner, .news-col, .faculties, .faculties-inner, .faculties-grid, .programs, .programs-inner, .faculty-grid, section.faculties, section.programs, section.news-events")) return;
            img.classList.add("cms-wrapped");
            var wrap = img.parentElement;
            if (!wrap || !wrap.classList.contains("cms-img-wrap")) {
                wrap = document.createElement("span");
                wrap.className = "cms-img-wrap";
                img.parentNode.insertBefore(wrap, img);
                wrap.appendChild(img);
            }

            enableLiveImagePan(img);

            var btnGroup = document.createElement("div");
            btnGroup.className = "cms-img-btn-group";

            var changeBtn = document.createElement("button");
            changeBtn.type = "button";
            changeBtn.className = "cms-action-btn";
            changeBtn.innerHTML = '<i class="fas fa-camera"></i> Change Image';
            changeBtn.addEventListener("click", function (e) {
                e.preventDefault();
                e.stopPropagation();
                openImagePickerModal({
                    title: "Change Image",
                    targetElement: img,
                    currentUrl: img.src,
                    currentPosition: img.style.objectPosition || "50% 50%",
                    allowRemove: true,
                    defaultTab: "upload",
                    onSelect: function (url, pos) {
                        img.src = url;
                        if (pos) img.style.objectPosition = pos;
                        setStatus("Image updated. Click Save Changes to publish.");
                    },
                    onRemove: function () {
                        img.src = "/images/logo-placeholder.svg";
                        setStatus("Image reset. Click Save Changes to publish.");
                    }
                });
            });
            btnGroup.appendChild(changeBtn);

            var removeBtn = document.createElement("button");
            removeBtn.type = "button";
            removeBtn.className = "cms-action-btn cms-btn-danger";
            removeBtn.innerHTML = '<i class="fas fa-trash-can"></i> Remove';
            removeBtn.addEventListener("click", function (e) {
                e.preventDefault();
                e.stopPropagation();
                if (confirm("Remove this image?")) {
                    img.src = "/images/logo-placeholder.svg";
                    setStatus("Image reset. Click Save Changes to publish.");
                }
            });
            btnGroup.appendChild(removeBtn);

            wrap.appendChild(btnGroup);

            setupDirectDragAndDrop(wrap, function (url, pos) {
                img.src = url;
                if (pos) img.style.objectPosition = pos;
                setStatus("Image updated. Click Save Changes to publish.");
            });
        });
    }

    // Gallery Placeholders: supports .gallery-item and .gallery-img-placeholder
    function applyImageToGalleryItem(item, url, pos) {
        var existingImg = item.querySelector("img.gallery-img, img:not(.cms-wrapped)");
        if (!existingImg) existingImg = item.querySelector("img");

        if (existingImg) {
            existingImg.src = url;
            existingImg.style.display = "block";
            existingImg.style.objectPosition = pos || "50% 50%";
            existingImg.style.width = "100%";
            existingImg.style.height = "100%";
            existingImg.style.objectFit = "cover";
            enableLiveImagePan(existingImg);
        } else {
            var img = document.createElement("img");
            img.src = url;
            img.alt = "Facility Gallery Image";
            img.className = "gallery-img";
            img.style.width = "100%";
            img.style.height = "100%";
            img.style.objectFit = "cover";
            img.style.objectPosition = pos || "50% 50%";
            img.style.display = "block";
            img.style.borderRadius = "inherit";

            enableLiveImagePan(img);

            var placeholder = item.querySelector(".gallery-img-placeholder");
            if (placeholder) {
                placeholder.style.display = "none";
            }
            var label = item.querySelector(".gallery-label");
            if (label) {
                item.insertBefore(img, label);
            } else {
                item.appendChild(img);
            }
        }
        item.classList.add("cms-has-image");
        updateGalleryItemButtons(item);
        setStatus("Gallery image updated. Click Save Changes to publish.");
    }

    function removeImageFromGalleryItem(item) {
        var img = item.querySelector("img.gallery-img, img");
        if (img) {
            img.remove();
        }
        var placeholder = item.querySelector(".gallery-img-placeholder");
        if (placeholder) {
            placeholder.style.display = "";
        } else {
            var newPlaceholder = document.createElement("div");
            newPlaceholder.className = "gallery-img-placeholder";
            newPlaceholder.innerHTML = '<i class="fas fa-image"></i><span style="font-size:12px;font-weight:600;">Image Placeholder</span>';
            var label = item.querySelector(".gallery-label");
            if (label) {
                item.insertBefore(newPlaceholder, label);
            } else {
                item.appendChild(newPlaceholder);
            }
        }
        item.classList.remove("cms-has-image");
        updateGalleryItemButtons(item);
        setStatus("Image removed and placeholder restored. Click Save Changes to publish.");
    }

    function updateGalleryItemButtons(item) {
        var oldGroup = item.querySelector(".cms-img-btn-group, .cms-placeholder-change-btn");
        if (oldGroup) oldGroup.remove();

        var currentImg = item.querySelector("img");
        var hasImg = currentImg && currentImg.src && !currentImg.src.endsWith("logo-placeholder.svg") && currentImg.style.display !== "none";

        if (hasImg && currentImg) {
            enableLiveImagePan(currentImg);
        }

        var btnGroup = document.createElement("div");
        btnGroup.className = "cms-img-btn-group";

        var changeBtn = document.createElement("button");
        changeBtn.type = "button";
        changeBtn.className = "cms-action-btn";
        changeBtn.innerHTML = hasImg ? '<i class="fas fa-camera"></i> Change Image' : '<i class="fas fa-camera"></i> Add Image';
        changeBtn.addEventListener("click", function (e) {
            e.preventDefault();
            e.stopPropagation();
            openImagePickerModal({
                title: hasImg ? "Change Gallery Image" : "Add Gallery Image",
                targetElement: item,
                currentUrl: currentImg ? currentImg.src : "",
                currentPosition: currentImg ? (currentImg.style.objectPosition || "50% 50%") : "50% 50%",
                allowRemove: hasImg,
                defaultTab: "upload",
                onSelect: function (url, pos) {
                    applyImageToGalleryItem(item, url, pos);
                },
                onRemove: function () {
                    removeImageFromGalleryItem(item);
                }
            });
        });
        btnGroup.appendChild(changeBtn);

        if (hasImg) {
            var removeBtn = document.createElement("button");
            removeBtn.type = "button";
            removeBtn.className = "cms-action-btn cms-btn-danger";
            removeBtn.innerHTML = '<i class="fas fa-trash-can"></i> Remove';
            removeBtn.addEventListener("click", function (e) {
                e.preventDefault();
                e.stopPropagation();
                if (confirm("Remove this image and restore the placeholder?")) {
                    removeImageFromGalleryItem(item);
                }
            });
            btnGroup.appendChild(removeBtn);
        }

        item.appendChild(btnGroup);
    }

    function wrapGalleryPlaceholders() {
        var items = region.querySelectorAll(".gallery-item, .gallery-img-placeholder");
        items.forEach(function (el) {
            var item = el.classList.contains("gallery-item") ? el : (el.closest(".gallery-item") || el);
            if (item.classList.contains("cms-placeholder-wrapped")) return;
            item.classList.add("cms-placeholder-wrapped");

            var img = item.querySelector("img");
            if (img) {
                enableLiveImagePan(img);
            }

            updateGalleryItemButtons(item);

            setupDirectDragAndDrop(item, function (url, pos) {
                applyImageToGalleryItem(item, url, pos);
            });
        });
    }

    // ===== HERO SLIDES MANAGEMENT & EDIT CONTROLS =====
    function setupHeroSlideControls() {
        var hero = region.querySelector(".hero");
        if (!hero) return;

        // Clean up any old slide action bars
        hero.querySelectorAll(".cms-slide-action-bar").forEach(function (bar) { bar.remove(); });

        // Floating Hero Control Bar (Manage Slides, Add Slide)
        var existingControls = hero.querySelector(".cms-hero-controls");
        if (!existingControls) {
            var controls = document.createElement("div");
            controls.className = "cms-hero-controls";
            controls.setAttribute("contenteditable", "false");

            var manageBtn = document.createElement("button");
            manageBtn.type = "button";
            manageBtn.className = "cms-hero-ctrl-btn cms-btn-accent";
            manageBtn.innerHTML = '<i class="fas fa-sliders-h"></i> Manage Hero Slides';
            manageBtn.addEventListener("click", function (e) {
                e.preventDefault();
                e.stopPropagation();
                openHeroManagerModal();
            });

            var addSlideBtn = document.createElement("button");
            addSlideBtn.type = "button";
            addSlideBtn.className = "cms-hero-ctrl-btn";
            addSlideBtn.innerHTML = '<i class="fas fa-plus"></i> Add Slide';
            addSlideBtn.addEventListener("click", function (e) {
                e.preventDefault();
                e.stopPropagation();
                promptAddHeroSlide();
            });

            controls.appendChild(manageBtn);
            controls.appendChild(addSlideBtn);
            hero.appendChild(controls);
        }
    }

    function changeSlideMedia(slide, cb) {
        var hasVideo = !!slide.querySelector("video");
        if (hasVideo) {
            activeHeroVideoSlide = slide;
            activeVideoEl = slide.querySelector("video");
            if (videoInput) {
                videoInput._onComplete = cb;
                videoInput.click();
            }
        } else {
            var currentBg = slide.style.backgroundImage || "";
            var match = currentBg.match(/url\(['"]?(.*?)['"]?\)/);
            openImagePickerModal({
                title: "Change Slide Image",
                targetElement: slide,
                currentUrl: match ? match[1] : "",
                currentPosition: slide.style.backgroundPosition || "50% 50%",
                allowRemove: false,
                defaultTab: "upload",
                onSelect: function (url, pos) {
                    slide.style.backgroundImage = "url('" + url + "')";
                    if (pos) slide.style.backgroundPosition = pos;
                    if (window.refreshHeroSlideshow) window.refreshHeroSlideshow();
                    setStatus("Slide image updated. Click Save Changes to publish.");
                    if (cb) cb();
                }
            });
        }
    }

    function deleteHeroSlide(slide) {
        var hero = region.querySelector(".hero");
        if (!hero) return;
        var slides = Array.from(hero.querySelectorAll(".hero-slide"));
        if (slides.length <= 1) {
            alert("The hero slider must have at least one slide.");
            return;
        }

        var slideIdx = slides.indexOf(slide);
        if (!confirm("Are you sure you want to delete Slide #" + (slideIdx + 1) + "?")) return;

        slide.remove();
        var nextIdx = Math.max(0, slideIdx - 1);
        if (window.refreshHeroSlideshow) window.refreshHeroSlideshow(nextIdx);
        setStatus("Slide deleted. Click Save Changes to publish.");
    }

    function promptAddHeroSlide(cb) {
        openImagePickerModal({
            title: "Add New Hero Slide Image",
            currentUrl: "",
            defaultTab: "upload",
            onSelect: function (url, pos) {
                var heroSlides = region.querySelector(".hero-slides");
                if (!heroSlides) return;

                var newSlide = document.createElement("div");
                newSlide.className = "hero-slide";
                newSlide.style.backgroundImage = "url('" + url + "')";
                if (pos) newSlide.style.backgroundPosition = pos;

                var inner = document.createElement("div");
                inner.className = "slide-bg-inner";
                newSlide.appendChild(inner);

                heroSlides.appendChild(newSlide);

                var allSlides = heroSlides.querySelectorAll(".hero-slide");
                if (window.refreshHeroSlideshow) {
                    window.refreshHeroSlideshow(allSlides.length - 1);
                }
                setStatus("New slide added! Click Save Changes to publish.");
                if (cb) cb();
            }
        });
    }

    function openHeroManagerModal() {
        var hero = region.querySelector(".hero");
        if (!hero) return;
        var heroSlides = hero.querySelector(".hero-slides");
        if (!heroSlides) return;

        var existingModal = document.getElementById("cmsHeroManagerModalOverlay");
        if (existingModal) existingModal.remove();

        var overlay = document.createElement("div");
        overlay.id = "cmsHeroManagerModalOverlay";
        overlay.className = "cms-hsm-modal-overlay";

        var card = document.createElement("div");
        card.className = "cms-hsm-modal-card";

        function renderModalContent() {
            var slides = Array.from(heroSlides.querySelectorAll(".hero-slide"));

            card.innerHTML = 
                '<div class="cms-hsm-modal-header">' +
                    '<h3><i class="fas fa-sliders-h"></i> Manage Hero Slides <span class="cms-hsm-count">' + slides.length + ' Slides</span></h3>' +
                    '<button type="button" class="cms-hsm-close-btn" id="cmsHsmCloseBtn"><i class="fas fa-times"></i></button>' +
                '</div>' +
                '<div class="cms-hsm-modal-body" id="cmsHsmBody"></div>' +
                '<div class="cms-hsm-modal-footer">' +
                    '<div class="cms-hsm-footer-btns">' +
                        '<button type="button" class="cms-hsm-main-btn cms-hsm-btn-primary" id="cmsHsmAddImgBtn"><i class="fas fa-plus"></i> Add Image Slide</button>' +
                        '<button type="button" class="cms-hsm-main-btn cms-hsm-btn-secondary" id="cmsHsmAddVidBtn"><i class="fas fa-video"></i> Add Video Slide</button>' +
                    '</div>' +
                    '<button type="button" class="cms-hsm-main-btn cms-hsm-btn-done" id="cmsHsmDoneBtn">Done</button>' +
                '</div>';

            var body = card.querySelector("#cmsHsmBody");

            slides.forEach(function (slide, idx) {
                var vid = slide.querySelector("video");
                var isVideo = !!vid;
                var bg = slide.style.backgroundImage || "";
                var match = bg.match(/url\(['"]?(.*?)['"]?\)/);
                var imgUrl = match ? match[1] : "";
                var vidSrc = vid ? (vid.getAttribute("src") || vid.src || "") : "";
                var isHidden = slide.getAttribute("data-cms-hidden") === "1" || slide.style.display === "none";

                var item = document.createElement("div");
                item.className = "cms-hsm-slide-card" + (slide.classList.contains("active") ? " active-preview" : "") + (isHidden ? " cms-slide-card-hidden" : "");

                var thumbStyle = !isVideo && imgUrl ? 'style="background-image:url(\'' + imgUrl + '\')"' : '';
                var thumbContent = isVideo ? '<i class="fas fa-play-circle cms-hsm-thumb-video"></i>' : (!imgUrl ? '<i class="fas fa-image" style="color:rgba(255,255,255,0.5);font-size:20px;"></i>' : '');

                var typeBadgeHtml = '<div class="cms-hsm-slide-type ' + (isVideo ? 'cms-hsm-type-video' : 'cms-hsm-type-image') + '">' +
                                        '<i class="' + (isVideo ? 'fas fa-video' : 'fas fa-image') + '"></i> ' + (isVideo ? 'Video Slide' : 'Image Slide') +
                                    '</div>';
                if (isHidden) {
                    typeBadgeHtml += '<span style="background:#fee2e2;color:#dc2626;font-size:10px;font-weight:700;padding:2px 6px;border-radius:4px;margin-left:6px;"><i class="fas fa-eye-slash"></i> Hidden</span>';
                }

                item.innerHTML = 
                    '<div class="cms-hsm-slide-num">#' + (idx + 1) + '</div>' +
                    '<div class="cms-hsm-thumb-wrap" ' + thumbStyle + '>' + thumbContent + '</div>' +
                    '<div class="cms-hsm-slide-info">' +
                        '<div style="display:flex;align-items:center;gap:4px;">' + typeBadgeHtml + '</div>' +
                        '<div class="cms-hsm-slide-src" title="' + (isVideo ? vidSrc : imgUrl) + '">' +
                            (isVideo ? (vidSrc ? vidSrc.split("/").pop() : "No video source") : (imgUrl ? imgUrl.split("/").pop() : "No image selected")) +
                        '</div>' +
                    '</div>' +
                    '<div class="cms-hsm-slide-actions">' +
                        '<button type="button" class="cms-hsm-action-btn cms-action-hide" title="' + (isHidden ? "Show Slide" : "Hide Slide") + '">' +
                            '<i class="' + (isHidden ? 'fas fa-eye-slash' : 'fas fa-eye') + '" style="' + (isHidden ? 'color:#dc2626;' : '') + '"></i>' +
                        '</button>' +
                        '<button type="button" class="cms-hsm-action-btn cms-action-change" title="Change Image/Video"><i class="fas fa-camera"></i></button>' +
                        (idx > 0 ? '<button type="button" class="cms-hsm-action-btn cms-action-up" title="Move Up"><i class="fas fa-arrow-up"></i></button>' : '') +
                        (idx < slides.length - 1 ? '<button type="button" class="cms-hsm-action-btn cms-action-down" title="Move Down"><i class="fas fa-arrow-down"></i></button>' : '') +
                        '<button type="button" class="cms-hsm-action-btn cms-action-del" title="Delete Slide"><i class="fas fa-trash-alt"></i></button>' +
                    '</div>';

                item.querySelector(".cms-action-hide").addEventListener("click", function () {
                    if (isHidden) {
                        slide.removeAttribute("data-cms-hidden");
                        slide.style.display = "";
                        setStatus("Slide #" + (idx + 1) + " is now visible. Click Save Changes to publish.");
                    } else {
                        slide.setAttribute("data-cms-hidden", "1");
                        slide.style.display = "none";
                        setStatus("Slide #" + (idx + 1) + " hidden. Click Save Changes to publish.");
                    }
                    if (window.refreshHeroSlideshow) window.refreshHeroSlideshow(idx);
                    renderModalContent();
                });

                item.querySelector(".cms-action-change").addEventListener("click", function () {
                    changeSlideMedia(slide, function () {
                        renderModalContent();
                    });
                });

                var upBtn = item.querySelector(".cms-action-up");
                if (upBtn) {
                    upBtn.addEventListener("click", function () {
                        var prev = slide.previousElementSibling;
                        if (prev) {
                            heroSlides.insertBefore(slide, prev);
                            if (window.refreshHeroSlideshow) window.refreshHeroSlideshow(idx - 1);
                            renderModalContent();
                            setStatus("Slides reordered. Click Save Changes to publish.");
                        }
                    });
                }

                var downBtn = item.querySelector(".cms-action-down");
                if (downBtn) {
                    downBtn.addEventListener("click", function () {
                        var next = slide.nextElementSibling;
                        if (next) {
                            heroSlides.insertBefore(next, slide);
                            if (window.refreshHeroSlideshow) window.refreshHeroSlideshow(idx + 1);
                            renderModalContent();
                            setStatus("Slides reordered. Click Save Changes to publish.");
                        }
                    });
                }

                item.querySelector(".cms-action-del").addEventListener("click", function () {
                    if (slides.length <= 1) {
                        alert("The hero slider must have at least one slide.");
                        return;
                    }
                    if (!confirm("Delete Slide #" + (idx + 1) + "?")) return;
                    slide.remove();
                    if (window.refreshHeroSlideshow) window.refreshHeroSlideshow(Math.max(0, idx - 1));
                    renderModalContent();
                    setStatus("Slide deleted. Click Save Changes to publish.");
                });

                body.appendChild(item);
            });

            // Close listeners
            card.querySelector("#cmsHsmCloseBtn").addEventListener("click", function () { overlay.remove(); });
            card.querySelector("#cmsHsmDoneBtn").addEventListener("click", function () { overlay.remove(); });

            // Add Image button
            card.querySelector("#cmsHsmAddImgBtn").addEventListener("click", function () {
                promptAddHeroSlide(function () {
                    renderModalContent();
                });
            });

            // Add Video button
            card.querySelector("#cmsHsmAddVidBtn").addEventListener("click", function () {
                if (videoInput) {
                    activeHeroVideoSlide = null;
                    activeVideoEl = null;
                    videoInput._isAddingHeroVideoSlide = true;
                    videoInput._onComplete = function () {
                        renderModalContent();
                    };
                    videoInput.click();
                }
            });
        }

        renderModalContent();
        overlay.appendChild(card);
        document.body.appendChild(overlay);

        overlay.addEventListener("click", function (e) {
            if (e.target === overlay) overlay.remove();
        });
    }

    // Wrap background-image elements (non-hero) with modal & drag & drop support
    function wrapBackgroundSlides() {
        var candidates = region.querySelectorAll("[data-cms-bg='1']:not(.cms-bg-wrapped)");
        candidates.forEach(function (el) {
            if (el.querySelector("video")) return;

            el.classList.add("cms-bg-wrapped", "cms-bg-wrap");

            var btnGroup = document.createElement("div");
            btnGroup.className = "cms-img-btn-group";

            var changeBtn = document.createElement("button");
            changeBtn.type = "button";
            changeBtn.className = "cms-action-btn";
            changeBtn.innerHTML = '<i class="fas fa-camera"></i> Change image';
            changeBtn.addEventListener("click", function (e) {
                e.preventDefault();
                e.stopPropagation();
                var currentBg = el.style.backgroundImage || "";
                var match = currentBg.match(/url\(['"]?(.*?)['"]?\)/);
                openImagePickerModal({
                    title: "Change Background Image",
                    targetElement: el,
                    currentUrl: match ? match[1] : "",
                    currentPosition: el.style.backgroundPosition || "50% 50%",
                    allowRemove: true,
                    defaultTab: "upload",
                    onSelect: function (url, pos) {
                        el.style.backgroundImage = "url('" + url + "')";
                        if (pos) el.style.backgroundPosition = pos;
                        setStatus("Background updated. Click Save Changes to publish.");
                    },
                    onRemove: function () {
                        el.style.backgroundImage = "none";
                        setStatus("Background removed. Click Save Changes to publish.");
                    }
                });
            });
            btnGroup.appendChild(changeBtn);

            el.appendChild(btnGroup);

            setupDirectDragAndDrop(el, function (url, pos) {
                el.style.backgroundImage = "url('" + url + "')";
                if (pos) el.style.backgroundPosition = pos;
                setStatus("Background updated. Click Save Changes to publish.");
            });
        });
    }

    // Wrap <video> elements with replacement button
    function wrapVideoSlides() {
        if (!videoInput) return;
        var videos = region.querySelectorAll("video:not(.cms-video-wrapped)");
        videos.forEach(function (video) {
            if (video.closest(".hero-slide")) return; // handled by hero slide manager
            video.classList.add("cms-video-wrapped");
            var wrap = video.parentElement;
            if (!wrap || !wrap.classList.contains("cms-video-wrap")) {
                wrap = document.createElement("span");
                wrap.className = "cms-video-wrap";
                video.parentNode.insertBefore(wrap, video);
                wrap.appendChild(video);
            }

            var btn = document.createElement("button");
            btn.type = "button";
            btn.className = "cms-video-change-btn";
            btn.innerHTML = '<i class="fas fa-video"></i> Change video';
            btn.addEventListener("click", function (e) {
                e.preventDefault();
                e.stopPropagation();
                activeVideoEl = video;
                videoInput.click();
            });
            wrap.appendChild(btn);
        });
    }

    function processVideoUploadResponse(r) {
        return r.json().catch(function () {
            if (r.status === 413) {
                throw new Error("File is too large. Server limit is 300 MB.");
            }
            throw new Error("Server response error (" + r.status + "). Please check server logs.");
        }).then(function (data) {
            if (!r.ok || (data && data.success === false)) {
                throw new Error((data && data.message) || "Upload failed (" + r.status + ")");
            }
            return data;
        });
    }

    if (videoInput) {
        videoInput.addEventListener("change", function () {
            var file = videoInput.files[0];
            if (!file) return;

            if (videoInput._isAddingHeroVideoSlide) {
                videoInput._isAddingHeroVideoSlide = false;
                setStatus("Uploading video slide... this may take a moment.");
                var fd = new FormData();
                fd.append("file", file);
                fd.append("__RequestVerificationToken", antiForgeryToken());

                fetch("/Cms/UploadVideo", { method: "POST", body: fd })
                    .then(processVideoUploadResponse)
                    .then(function (data) {
                        var heroSlides = region.querySelector(".hero-slides");
                        if (heroSlides) {
                            var newSlide = document.createElement("div");
                            newSlide.className = "hero-slide";
                            newSlide.innerHTML = 
                                '<div class="hero-video-slide">' +
                                    '<div class="hero-video-fallback"><i class="fas fa-plane"></i></div>' +
                                    '<video autoplay muted loop playsinline style="position:absolute;top:0;left:0;width:100%;height:100%;object-fit:cover;z-index:1;" src="' + data.url + '"></video>' +
                                '</div>';
                            heroSlides.appendChild(newSlide);
                            var allSlides = heroSlides.querySelectorAll(".hero-slide");
                            if (window.refreshHeroSlideshow) window.refreshHeroSlideshow(allSlides.length - 1);
                            setStatus("Video slide added. Click Save Changes to publish.");
                        }
                    })
                    .catch(function (err) {
                        console.error("Video Upload Error:", err);
                        setStatus(err.message || "Upload failed. Check your connection or file format.", true);
                    })
                    .finally(function () {
                        videoInput.value = "";
                        if (videoInput._onComplete) {
                            var cb = videoInput._onComplete;
                            videoInput._onComplete = null;
                            cb();
                        }
                    });
                return;
            }

            if (!activeVideoEl && !activeHeroVideoSlide) return;

            setStatus("Uploading video... this may take a moment.");
            var fd = new FormData();
            fd.append("file", file);
            fd.append("__RequestVerificationToken", antiForgeryToken());

            fetch("/Cms/UploadVideo", { method: "POST", body: fd })
                .then(processVideoUploadResponse)
                .then(function (data) {
                    if (!activeVideoEl && activeHeroVideoSlide) {
                        activeVideoEl = document.createElement("video");
                        activeVideoEl.id = "heroVideo";
                        activeVideoEl.autoplay = true;
                        activeVideoEl.muted = true;
                        activeVideoEl.loop = true;
                        activeVideoEl.setAttribute("playsinline", "");
                        activeVideoEl.style.position = "absolute";
                        activeVideoEl.style.top = "0";
                        activeVideoEl.style.left = "0";
                        activeVideoEl.style.width = "100%";
                        activeVideoEl.style.height = "100%";
                        activeVideoEl.style.objectFit = "cover";
                        activeVideoEl.style.zIndex = "1";
                        var videoSlideWrap = activeHeroVideoSlide.querySelector(".hero-video-slide");
                        if (videoSlideWrap) {
                            videoSlideWrap.appendChild(activeVideoEl);
                        } else {
                            activeHeroVideoSlide.appendChild(activeVideoEl);
                        }
                    }
                    activeVideoEl.querySelectorAll("source").forEach(function (s) { s.remove(); });
                    activeVideoEl.src = data.url;
                    activeVideoEl.load();
                    if (window.refreshHeroSlideshow) window.refreshHeroSlideshow();
                    setStatus("Video updated. Click Save Changes to publish.");
                })
                .catch(function (err) {
                    console.error("Video Upload Error:", err);
                    setStatus(err.message || "Upload failed. Check your connection or file format.", true);
                })
                .finally(function () {
                    videoInput.value = "";
                    activeVideoEl = null;
                    activeHeroVideoSlide = null;
                    if (videoInput._onComplete) {
                        var cb = videoInput._onComplete;
                        videoInput._onComplete = null;
                        cb();
                    }
                });
        });
    }

    // Prevent accidental navigation on editable body links while in edit mode
    region.addEventListener("click", function (e) {
        if (!document.body.classList.contains("cms-editing")) return;
        var link = e.target.closest("a");
        if (link && !link.closest(".announcement, nav.navbar, .navbar, .menu-item, .dropdown-menu, .cms-toolbar, .cms-mode-banner, .cms-hero-controls, .cms-hsm-modal-overlay")) {
            e.preventDefault();
        }
    });

    function wrapIcons() {
        region.querySelectorAll("i.fas, i.fab, i.far, i.fa-solid, i.fa-brands, i.fa-regular").forEach(function (icon) {
            if (icon.closest(".announcement, nav.navbar, .cms-toolbar, .cms-mode-banner, .nau-icon-modal, .cms-hero-controls, .cms-slide-action-bar, .cms-hsm-modal-overlay, .news-events, .news-events-inner, .news-col, .faculties, .faculties-inner, .faculties-grid, .programs, .programs-inner, .faculty-grid, section.faculties, section.programs, section.news-events, .vmv-section, section.vmv-section")) return;
            icon.classList.add("cms-icon-editable");
            icon.setAttribute("title", "Click to change icon (" + icon.className + ")");
        });
    }

    function enterEditMode() {
        wrapImages();
        setupHeroSlideControls();
        wrapBackgroundSlides();
        wrapGalleryPlaceholders();
        wrapVideoSlides();
        wrapIcons();

        // Enable contenteditable ONLY on text headings, paragraphs, and labels (never on container)
        region.querySelectorAll("h1, h2, h3, h4, h5, h6, p, .hero h1, .hero p, .label, .r-name, .r-title, .hero-btn-primary, .hero-btn-secondary, .feat-title, .feat-desc, .feat-label, .dd-col-title, .dd-text strong, .dd-text span, .stat-num, .stat-lbl, .event-title, .news-title, .rector-regards, .rector-content div, .fact-number, .fact-label, .fact-item div").forEach(function (el) {
            if (el.closest(".announcement, nav.navbar, .cms-toolbar, .cms-mode-banner, .cms-hero-controls, .cms-slide-action-bar, .cms-hsm-modal-overlay, .slide-arrow, .slide-dots, .news-events, .news-events-inner, .news-col, .faculties, .faculties-inner, .faculties-grid, .programs, .programs-inner, .faculty-grid, section.faculties, section.programs, section.news-events, .vmv-section, section.vmv-section")) return;
            el.setAttribute("contenteditable", "true");
            el.classList.add("cms-text-editable");

            el.addEventListener("focus", function () { showTextFormatToolbar(this); });
            el.addEventListener("click", function (e) {
                e.stopPropagation();
                showTextFormatToolbar(this);
            });
        });

        document.body.classList.add("cms-editing");
        editBtn.style.display = "none";
        saveBtn.style.display = "inline-flex";
        cancelBtn.style.display = "inline-flex";
        setStatus("Edit mode on: click text to format Font Size & Color inline.");
    }

    // ===== FLOATING TEXT FORMATTING TOOLBAR =====
    var activeEditableElement = null;
    var formatToolbarEl = null;

    function initTextFormatToolbar() {
        if (document.getElementById("cmsTextFormatToolbar")) {
            formatToolbarEl = document.getElementById("cmsTextFormatToolbar");
            return;
        }

        formatToolbarEl = document.createElement("div");
        formatToolbarEl.id = "cmsTextFormatToolbar";
        formatToolbarEl.className = "cms-text-format-toolbar";
        formatToolbarEl.setAttribute("contenteditable", "false");

        formatToolbarEl.innerHTML = [
            '<div class="cms-tf-group">',
            '  <select class="cms-tf-select" id="cmsTfFontSize" title="Font Size">',
            '    <option value="">Size (Default)</option>',
            '    <option value="12px">12 px</option>',
            '    <option value="13px">13 px</option>',
            '    <option value="14px">14 px</option>',
            '    <option value="16px">16 px (Standard)</option>',
            '    <option value="18px">18 px</option>',
            '    <option value="20px">20 px</option>',
            '    <option value="24px">24 px (Subhead)</option>',
            '    <option value="28px">28 px (Title)</option>',
            '    <option value="32px">32 px (Heading)</option>',
            '    <option value="36px">36 px (Large)</option>',
            '    <option value="42px">42 px (Hero)</option>',
            '  </select>',
            '</div>',
            '<div class="cms-tf-divider"></div>',
            '<div class="cms-tf-group" title="Text Color">',
            '  <div class="cms-tf-color-picker-wrap">',
            '    <input type="color" class="cms-tf-color-input" id="cmsTfColorInput" value="#0b2c4d">',
            '    <i class="fas fa-palette" style="color:#38bdf8; font-size:12px;"></i>',
            '  </div>',
            '  <div class="cms-tf-swatch" data-color="#0b2c4d" style="background:#0b2c4d;" title="Navy"></div>',
            '  <div class="cms-tf-swatch" data-color="#16a34a" style="background:#16a34a;" title="Green"></div>',
            '  <div class="cms-tf-swatch" data-color="#0284c7" style="background:#0284c7;" title="Sky Blue"></div>',
            '  <div class="cms-tf-swatch" data-color="#1e293b" style="background:#1e293b;" title="Dark Slate"></div>',
            '  <div class="cms-tf-swatch" data-color="#dc2626" style="background:#dc2626;" title="Red"></div>',
            '</div>',
            '<div class="cms-tf-divider"></div>',
            '<div class="cms-tf-group">',
            '  <button type="button" class="cms-tf-btn" data-action="bold" title="Bold (Ctrl+B)"><i class="fas fa-bold"></i></button>',
            '  <button type="button" class="cms-tf-btn" data-action="italic" title="Italic (Ctrl+I)"><i class="fas fa-italic"></i></button>',
            '  <button type="button" class="cms-tf-btn" data-action="underline" title="Underline (Ctrl+U)"><i class="fas fa-underline"></i></button>',
            '</div>',
            '<div class="cms-tf-divider"></div>',
            '<div class="cms-tf-group">',
            '  <button type="button" class="cms-tf-btn" data-align="left" title="Align Left"><i class="fas fa-align-left"></i></button>',
            '  <button type="button" class="cms-tf-btn" data-align="center" title="Align Center"><i class="fas fa-align-center"></i></button>',
            '  <button type="button" class="cms-tf-btn" data-align="right" title="Align Right"><i class="fas fa-align-right"></i></button>',
            '</div>',
            '<div class="cms-tf-divider"></div>',
            '<button type="button" class="cms-tf-btn cms-tf-close" title="Hide Toolbar"><i class="fas fa-times"></i></button>'
        ].join('');

        document.body.appendChild(formatToolbarEl);

        formatToolbarEl.addEventListener("mousedown", function (e) {
            if (e.target.tagName !== "SELECT" && e.target.tagName !== "INPUT") {
                e.preventDefault();
            }
        });

        document.getElementById("cmsTfFontSize").addEventListener("change", function (e) {
            var size = e.target.value;
            if (!size) return;
            applyTextStyle("fontSize", size);
        });

        document.getElementById("cmsTfColorInput").addEventListener("input", function (e) {
            applyTextStyle("color", e.target.value);
        });

        formatToolbarEl.querySelectorAll(".cms-tf-swatch").forEach(function (swatch) {
            swatch.addEventListener("click", function () {
                var color = this.getAttribute("data-color");
                document.getElementById("cmsTfColorInput").value = color;
                applyTextStyle("color", color);
            });
        });

        formatToolbarEl.querySelectorAll(".cms-tf-btn[data-action]").forEach(function (btn) {
            btn.addEventListener("click", function () {
                var action = this.getAttribute("data-action");
                document.execCommand(action, false, null);
            });
        });

        formatToolbarEl.querySelectorAll(".cms-tf-btn[data-align]").forEach(function (btn) {
            btn.addEventListener("click", function () {
                var align = this.getAttribute("data-align");
                if (activeEditableElement) {
                    activeEditableElement.style.textAlign = align;
                } else {
                    document.execCommand("justify" + align.charAt(0).toUpperCase() + align.slice(1), false, null);
                }
            });
        });

        formatToolbarEl.querySelector(".cms-tf-close").addEventListener("click", function () {
            hideTextFormatToolbar();
        });
    }

    function applyTextStyle(styleProp, value) {
        var sel = window.getSelection();
        if (sel && sel.rangeCount > 0 && !sel.isCollapsed) {
            try {
                var range = sel.getRangeAt(0);
                var span = document.createElement("span");
                span.style[styleProp] = value;
                span.appendChild(range.extractContents());
                range.insertNode(span);
                return;
            } catch (e) { }
        }
        if (activeEditableElement) {
            activeEditableElement.style[styleProp] = value;
        }
    }

    function showTextFormatToolbar(el) {
        if (!formatToolbarEl) initTextFormatToolbar();
        activeEditableElement = el;

        var rect = el.getBoundingClientRect();
        var toolbarW = 420;
        var left = rect.left + (rect.width / 2) - (toolbarW / 2) + window.scrollX;
        var top = rect.top + window.scrollY - 48;

        if (top < window.scrollY + 10) {
            top = rect.bottom + window.scrollY + 8;
        }
        if (left < 10) left = 10;

        formatToolbarEl.style.top = top + "px";
        formatToolbarEl.style.left = left + "px";
        formatToolbarEl.classList.add("visible");
    }

    function hideTextFormatToolbar() {
        if (formatToolbarEl) {
            formatToolbarEl.classList.remove("visible");
        }
        activeEditableElement = null;
    }

    document.addEventListener("click", function (e) {
        if (!document.body.classList.contains("cms-editing")) return;
        if (e.target.closest(".cms-text-editable, #cmsTextFormatToolbar, .cms-toolbar, .cms-mode-banner")) return;
        hideTextFormatToolbar();
    });

    function exitEditMode() {
        hideTextFormatToolbar();
        region.querySelectorAll("[contenteditable]").forEach(function (el) {
            el.removeAttribute("contenteditable");
            el.classList.remove("cms-text-editable");
        });
        document.body.classList.remove("cms-editing");
        region.querySelectorAll(".cms-icon-editable").forEach(function (el) {
            el.classList.remove("cms-icon-editable");
        });
        editBtn.style.display = "inline-flex";
        saveBtn.style.display = "none";
        cancelBtn.style.display = "none";
    }

    function cleanedHtml() {
        var clone = region.cloneNode(true);
        clone.removeAttribute("contenteditable");

        // Strip announcement and navbars
        clone.querySelectorAll(".announcement, nav.navbar, div.navbar").forEach(function (el) {
            el.remove();
        });

        // Strip CMS UI buttons and slide controls
        clone.querySelectorAll(".cms-img-btn-group, .cms-action-btn, .cms-placeholder-change-btn, .cms-img-change-btn, .cms-bg-change-btn, .cms-video-change-btn, .cms-remove-btn, .cms-add-btn, .cms-hero-controls, .cms-slide-action-bar, .cms-hsm-modal-overlay, .cms-text-format-toolbar, #cmsTextFormatToolbar, .vmv-section-edit-btn, .vmv-edit-modal-backdrop").forEach(function (btn) {
            btn.remove();
        });

        // Clear dynamic slide dots so static dots are NEVER stored in DB HTML
        clone.querySelectorAll("#slideDots, .slide-dots").forEach(function (dots) {
            dots.innerHTML = "";
        });

        clone.querySelectorAll(".cms-img-wrap").forEach(function (wrap) {
            var img = wrap.querySelector("img");
            if (img) {
                img.classList.remove("cms-wrapped", "cms-drag-target", "cms-drag-active");
                wrap.parentNode.replaceChild(img, wrap);
            }
        });

        clone.querySelectorAll(".cms-bg-wrap, .cms-bg-wrapped, .cms-placeholder-wrapped, .cms-drag-target, .cms-drag-active, .cms-has-image").forEach(function (el) {
            el.classList.remove("cms-bg-wrap", "cms-bg-wrapped", "cms-placeholder-wrapped", "cms-drag-target", "cms-drag-active", "cms-has-image");
        });

        clone.querySelectorAll(".cms-video-wrap").forEach(function (wrap) {
            var video = wrap.querySelector("video");
            if (video) {
                video.classList.remove("cms-video-wrapped");
                wrap.parentNode.replaceChild(video, wrap);
            }
        });
        clone.querySelectorAll(".cms-hero-video-wrapped").forEach(function (el) {
            el.classList.remove("cms-hero-video-wrapped");
        });

        clone.querySelectorAll(".cms-item-pos").forEach(function (el) {
            el.classList.remove("cms-item-pos");
        });

        clone.querySelectorAll("[contenteditable]").forEach(function (el) {
            el.removeAttribute("contenteditable");
        });

        clone.querySelectorAll(".cms-icon-editable").forEach(function (el) {
            el.classList.remove("cms-icon-editable");
            if (el.getAttribute("title") && el.getAttribute("title").indexOf("Click to change icon") !== -1) {
                el.removeAttribute("title");
            }
        });

        clone.querySelectorAll(".account-dropdown").forEach(function (wrap) {
            var btn = wrap.querySelector(".apply-btn");
            if (btn) {
                btn.textContent = "Account";
                btn.setAttribute("href", "#");
                wrap.parentNode.replaceChild(btn, wrap);
            }
        });

        return clone.innerHTML;
    }

    if (region._cmsInitialized) {
        return region._cmsEditorInstance;
    }
    region._cmsInitialized = true;

    function getCleanUrl() {
        try {
            var url = new URL(window.location.href);
            url.searchParams.delete("edit");
            var cleanPath = url.pathname + (url.searchParams.toString() ? "?" + url.searchParams.toString() : "") + url.hash;
            return cleanPath || "/";
        } catch (e) {
            return window.location.pathname || "/";
        }
    }

    editBtn.addEventListener("click", enterEditMode);

    cancelBtn.addEventListener("click", function (e) {
        e.preventDefault();
        if (confirm("Discard unsaved changes?")) {
            window.location.href = getCleanUrl();
        }
    });

    // Returns a Promise<{success, message}> instead of reloading itself, so
    // a page-level controller (e.g. the About page's single "Save Changes"
    // button) can await several of these together and only reload once,
    // after every section has actually finished saving.
    function doSave() {
        setStatus("Saving...");
        var fd = new FormData();
        fd.append("html", cleanedHtml());
        var pageSlug = region.getAttribute("data-page-slug") || (ids && ids.slug) || "";
        if (pageSlug) fd.append("slug", pageSlug);
        fd.append("__RequestVerificationToken", antiForgeryToken());

        return fetch(saveUrl, { method: "POST", body: fd })
            .then(function (r) { return r.json(); })
            .then(function (data) {
                if (data.success) {
                    setStatus("Saved.");
                    return { success: true };
                }
                setStatus(data.message || "Save failed.", true);
                return { success: false, message: data.message || "Save failed." };
            })
            .catch(function () {
                setStatus("Save failed. Check your connection.", true);
                return { success: false, message: "Save failed. Check your connection." };
            });
    }

    saveBtn.addEventListener("click", function (e) {
        e.preventDefault();
        saveBtn.disabled = true;
        doSave().then(function (result) {
            if (result.success) {
                setStatus("Saved! Redirecting...");
                setTimeout(function () {
                    window.location.href = getCleanUrl();
                }, 500);
            } else {
                saveBtn.disabled = false;
            }
        });
    });

    if (!ids || ids.autoEnter !== false) {
        enterEditMode();
    }

    var instance = {
        region: region,
        editBtn: editBtn,
        saveBtn: saveBtn,
        cancelBtn: cancelBtn,
        enterEditMode: enterEditMode,
        exitEditMode: exitEditMode,
        save: doSave
    };

    region._cmsEditorInstance = instance;
    return instance;
}

// Auto-init only if DOM is ready and hasn't been initialized yet
if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", function () {
        initCmsEditor();
    });
} else {
    initCmsEditor();
}
