/**
 * Button Visibility Fix
 * Ensures that action buttons are properly visible on all management pages
 */
(function($) {
    "use strict";

    $(document).ready(function() {
        console.log("Button fix script loaded");
        
        // Define direct navigation URLs for various pages
        const directUrls = {
            'Slot': '/Management/CreateParkingSlot',
            'Operator': '/Management/CreateOperator',
            'Shift': '/Management/CreateShift',
            'Kamera': '/Management/CreateCameraSetting',
            'Tarif': '/Rates/Create'
        };
        
        // Force correct display of add buttons by checking and ensuring proper visibility
        // This addresses issues with buttons not appearing on some management pages
        const addButtons = [
            ".btn-primary:contains('Tambah')",
            "a[href*='Create']",
            "a.btn-primary",
            "a[asp-action*='Create']"
        ];
        
        // Make buttons visible and ensure proper styling
        addButtons.forEach(selector => {
            $(selector).each(function() {
                console.log("Found button: ", $(this).text());
                $(this).css({
                    'display': 'inline-block !important',
                    'visibility': 'visible !important',
                    'opacity': '1 !important',
                    'position': 'relative !important',
                    'z-index': '1000 !important'
                });
                
                if (!$(this).hasClass('btn')) {
                    $(this).addClass('btn');
                }
                
                if (!$(this).hasClass('btn-primary')) {
                    $(this).addClass('btn-primary');
                }
                
                // If the button contains no icon, add a plus icon
                if ($(this).find('.fas, .fa, .far').length === 0) {
                    $(this).prepend('<i class="fas fa-plus me-1"></i> ');
                }
                
                // Add custom click handler to buttons with href issues
                const buttonText = $(this).text().trim();
                for (const [key, url] of Object.entries(directUrls)) {
                    if (buttonText.includes(key)) {
                        $(this).attr('href', url);
                        $(this).off('click').on('click', function(e) {
                            e.preventDefault();
                            console.log("Navigating to: " + url);
                            window.location.href = url;
                        });
                    }
                }
            });
        });
        
        // Check for Card Headers without action buttons and add them
        $(".card-header").each(function() {
            const title = $(this).text().trim();
            let actionAdded = false;
            
            // Check if this header already has action buttons
            if ($(this).find('a.btn, button.btn').length === 0) {
                // Add appropriate action button based on context
                if (title.includes("Tarif")) {
                    $(this).addClass('d-flex justify-content-between align-items-center');
                    const btn = $('<a href="/Rates/Create" class="btn btn-primary btn-sm"><i class="fas fa-plus me-1"></i> Tambah Tarif Baru</a>');
                    $(this).append(btn);
                    btn.on('click', function(e) {
                        e.preventDefault();
                        window.location.href = '/Rates/Create';
                    });
                    actionAdded = true;
                } else if (title.includes("Shift")) {
                    $(this).addClass('d-flex justify-content-between align-items-center');
                    const btn = $('<a href="/Management/CreateShift" class="btn btn-primary btn-sm"><i class="fas fa-plus me-1"></i> Tambah Shift Baru</a>');
                    $(this).append(btn);
                    btn.on('click', function(e) {
                        e.preventDefault();
                        window.location.href = '/Management/CreateShift';
                    });
                    actionAdded = true;
                } else if (title.includes("Operator")) {
                    $(this).addClass('d-flex justify-content-between align-items-center');
                    const btn = $('<a href="/Management/CreateOperator" class="btn btn-primary btn-sm"><i class="fas fa-plus me-1"></i> Tambah Operator</a>');
                    $(this).append(btn);
                    btn.on('click', function(e) {
                        e.preventDefault();
                        window.location.href = '/Management/CreateOperator';
                    });
                    actionAdded = true;
                } else if (title.includes("Slot")) {
                    $(this).addClass('d-flex justify-content-between align-items-center');
                    const btn = $('<a href="/Management/CreateParkingSlot" class="btn btn-primary btn-sm"><i class="fas fa-plus me-1"></i> Tambah Slot Baru</a>');
                    $(this).append(btn);
                    btn.on('click', function(e) {
                        e.preventDefault();
                        window.location.href = '/Management/CreateParkingSlot';
                    });
                    actionAdded = true;
                } else if (title.includes("Kamera")) {
                    $(this).addClass('d-flex justify-content-between align-items-center');
                    const btn = $('<a href="/Management/CreateCameraSetting" class="btn btn-primary btn-sm"><i class="fas fa-plus me-1"></i> Tambah Pengaturan Kamera</a>');
                    $(this).append(btn);
                    btn.on('click', function(e) {
                        e.preventDefault();
                        window.location.href = '/Management/CreateCameraSetting';
                    });
                    actionAdded = true;
                }
            }
            
            if (actionAdded) {
                console.log("Added action button to: " + title);
            }
        });
        
        // Special handling for problematic pages
        const currentPath = window.location.pathname.toLowerCase();
        
        if (currentPath.includes('/management/parkingslots')) {
            // Ensure ParkingSlots page has a working Add button
            if ($('.card-header a.btn-primary').length === 0) {
                console.log("Adding parking slot button to header");
                $('.card-header').append(
                    '<a href="/Management/CreateParkingSlot" class="btn btn-primary btn-sm" style="display:inline-block !important; visibility:visible !important; opacity:1 !important; margin-left: auto;">' +
                    '<i class="fas fa-plus me-1"></i> Tambah Slot Baru</a>'
                );
            }
            
            // Add a floating button in case all else fails
            if ($('#floating-add-btn').length === 0) {
                $('body').append(
                    '<a id="floating-add-btn" href="/Management/CreateParkingSlot" class="btn btn-primary" style="position:fixed; bottom:20px; right:20px; z-index:9999; border-radius:50%; width:60px; height:60px; display:flex; align-items:center; justify-content:center;">' +
                    '<i class="fas fa-plus fa-2x"></i></a>'
                );
            }
        }
        
        if (currentPath.includes('/management/camerasettings')) {
            // Fix for CameraSettings page
            $('a[asp-action="CreateCameraSetting"]').each(function() {
                console.log("Found camera setting add button, making it work");
                $(this).attr('href', '/Management/CreateCameraSetting');
                $(this).on('click', function(e) {
                    e.preventDefault();
                    window.location.href = '/Management/CreateCameraSetting';
                });
            });
            
            // Add floating button for backup
            if ($('#floating-add-btn').length === 0) {
                $('body').append(
                    '<a id="floating-add-btn" href="/Management/CreateCameraSetting" class="btn btn-primary" style="position:fixed; bottom:20px; right:20px; z-index:9999; border-radius:50%; width:60px; height:60px; display:flex; align-items:center; justify-content:center;">' +
                    '<i class="fas fa-plus fa-2x"></i></a>'
                );
            }
        }
        
        if (currentPath.includes('/management/operators')) {
            // Ensure Operators page has a working Add button
            if ($('a[asp-action="CreateOperator"]').length > 0) {
                $('a[asp-action="CreateOperator"]').attr('href', '/Management/CreateOperator');
            } else if ($('.card-header a.btn-primary').length === 0) {
                console.log("Adding operator button to header");
                $('.card-header').append(
                    '<a href="/Management/CreateOperator" class="btn btn-primary btn-sm" style="display:inline-block !important; visibility:visible !important; opacity:1 !important; margin-left: auto;">' +
                    '<i class="fas fa-plus me-1"></i> Tambah Operator</a>'
                );
            }
            
            // Add a floating button in case all else fails
            if ($('#floating-add-btn').length === 0) {
                $('body').append(
                    '<a id="floating-add-btn" href="/Management/CreateOperator" class="btn btn-primary" style="position:fixed; bottom:20px; right:20px; z-index:9999; border-radius:50%; width:60px; height:60px; display:flex; align-items:center; justify-content:center;">' +
                    '<i class="fas fa-plus fa-2x"></i></a>'
                );
            }
        }
        
        if (currentPath.includes('/management/shifts')) {
            // Ensure Shifts page has a working Add button
            if ($('.card-header a.btn-primary').length === 0) {
                console.log("Adding shift button to header");
                $('.card-header').append(
                    '<a href="/Management/CreateShift" class="btn btn-primary btn-sm" style="display:inline-block !important; visibility:visible !important; opacity:1 !important; margin-left: auto;">' +
                    '<i class="fas fa-plus me-1"></i> Tambah Shift Baru</a>'
                );
            }
            
            // Add a floating button in case all else fails
            if ($('#floating-add-btn').length === 0) {
                $('body').append(
                    '<a id="floating-add-btn" href="/Management/CreateShift" class="btn btn-primary" style="position:fixed; bottom:20px; right:20px; z-index:9999; border-radius:50%; width:60px; height:60px; display:flex; align-items:center; justify-content:center;">' +
                    '<i class="fas fa-plus fa-2x"></i></a>'
                );
            }
        }
        
        if (currentPath.includes('/rates')) {
            // Ensure Rates page has a working Add button
            if ($('.card-header a.btn-primary').length === 0) {
                console.log("Adding rates button to header");
                $('.card-header').append(
                    '<a href="/Rates/Create" class="btn btn-primary btn-sm" style="display:inline-block !important; visibility:visible !important; opacity:1 !important; margin-left: auto;">' +
                    '<i class="fas fa-plus me-1"></i> Tambah Tarif Baru</a>'
                );
            }
            
            // Add a floating button in case all else fails
            if ($('#floating-add-btn').length === 0) {
                $('body').append(
                    '<a id="floating-add-btn" href="/Rates/Create" class="btn btn-primary" style="position:fixed; bottom:20px; right:20px; z-index:9999; border-radius:50%; width:60px; height:60px; display:flex; align-items:center; justify-content:center;">' +
                    '<i class="fas fa-plus fa-2x"></i></a>'
                );
            }
        }
    });

})(jQuery); 