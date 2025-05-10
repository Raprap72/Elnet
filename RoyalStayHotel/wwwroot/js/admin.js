// Initialize DataTables
$(document).ready(function() {
    if ($.fn.DataTable) {
        $('.datatable').DataTable({
            responsive: true,
            pageLength: 10,
            order: [[0, 'desc']],
            language: {
                search: "_INPUT_",
                searchPlaceholder: "Search..."
            }
        });
    }

    // Initialize tooltips
    $('[data-toggle="tooltip"]').tooltip();

    // Auto-hide alerts after 5 seconds
    setTimeout(function() {
        $('.alert').alert('close');
    }, 5000);

    // Handle sidebar toggle
    $('#sidebarCollapse').on('click', function() {
        $('#sidebar').toggleClass('active');
    });

    // Handle responsive sidebar
    function handleResponsiveSidebar() {
        if (window.innerWidth < 768) {
            $('#sidebar').addClass('active');
        } else {
            $('#sidebar').removeClass('active');
        }
    }

    // Run on page load
    handleResponsiveSidebar();

    // Run on window resize
    $(window).resize(function() {
        handleResponsiveSidebar();
    });

    // Handle form validation
    $('form').on('submit', function(e) {
        if (this.checkValidity() === false) {
            e.preventDefault();
            e.stopPropagation();
        }
        $(this).addClass('was-validated');
    });

    // Handle delete confirmations
    $('.delete-confirm').on('click', function(e) {
        if (!confirm('Are you sure you want to delete this item?')) {
            e.preventDefault();
        }
    });

    // Handle status changes
    $('.status-change').on('change', function() {
        $(this).closest('form').submit();
    });

    // Handle file input
    $('.custom-file-input').on('change', function() {
        let fileName = $(this).val().split('\\').pop();
        $(this).next('.custom-file-label').addClass("selected").html(fileName);
    });

    // Handle date inputs
    if ($.fn.datepicker) {
        $('.datepicker').datepicker({
            format: 'yyyy-mm-dd',
            autoclose: true,
            todayHighlight: true
        });
    }

    // Handle select2 dropdowns
    if ($.fn.select2) {
        $('.select2').select2({
            theme: 'bootstrap4'
        });
    }

    // Handle number inputs
    $('.number-input').on('input', function() {
        this.value = this.value.replace(/[^0-9]/g, '');
    });

    // Handle decimal inputs
    $('.decimal-input').on('input', function() {
        this.value = this.value.replace(/[^0-9.]/g, '');
    });

    // Handle password toggle
    $('.password-toggle').on('click', function() {
        let input = $(this).closest('.input-group').find('input');
        if (input.attr('type') === 'password') {
            input.attr('type', 'text');
            $(this).find('i').removeClass('fa-eye').addClass('fa-eye-slash');
        } else {
            input.attr('type', 'password');
            $(this).find('i').removeClass('fa-eye-slash').addClass('fa-eye');
        }
    });

    // Handle image preview
    $('.image-input').on('change', function() {
        let input = this;
        if (input.files && input.files[0]) {
            let reader = new FileReader();
            reader.onload = function(e) {
                $(input).closest('.form-group').find('.image-preview').attr('src', e.target.result);
            }
            reader.readAsDataURL(input.files[0]);
        }
    });

    // Handle ajax form submission
    $('.ajax-form').on('submit', function(e) {
        e.preventDefault();
        let form = $(this);
        $.ajax({
            url: form.attr('action'),
            method: form.attr('method'),
            data: form.serialize(),
            success: function(response) {
                if (response.success) {
                    showNotification('success', response.message);
                    if (response.redirect) {
                        window.location.href = response.redirect;
                    }
                } else {
                    showNotification('error', response.message);
                }
            },
            error: function() {
                showNotification('error', 'An error occurred. Please try again.');
            }
        });
    });
});

// Show notification
function showNotification(type, message) {
    let alertClass = type === 'success' ? 'alert-success' : 'alert-danger';
    let icon = type === 'success' ? 'fa-check-circle' : 'fa-exclamation-circle';
    
    let alert = $('<div class="alert ' + alertClass + ' alert-dismissible fade show" role="alert">' +
        '<i class="fas ' + icon + ' mr-2"></i>' + message +
        '<button type="button" class="close" data-dismiss="alert" aria-label="Close">' +
        '<span aria-hidden="true">&times;</span>' +
        '</button>' +
        '</div>');
    
    $('#notifications').prepend(alert);
    
    setTimeout(function() {
        alert.alert('close');
    }, 5000);
} 