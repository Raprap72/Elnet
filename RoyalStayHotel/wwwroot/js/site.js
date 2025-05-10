// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
$(document).ready(function () {
    // Add smooth scrolling to all links
    $("a").on('click', function (event) {
        if (this.hash !== "") {
            event.preventDefault();
            var hash = this.hash;
            $('html, body').animate({
                scrollTop: $(hash).offset().top
            }, 800, function () {
                window.location.hash = hash;
            });
        }
    });

    // Initialize tooltips
    var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'))
    var tooltipList = tooltipTriggerList.map(function (tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl)
    });

    // Initialize datepickers if jQuery UI is available
    if ($.fn.datepicker) {
        $('.datepicker').datepicker({
            dateFormat: 'dd-mm-yy',
            minDate: 0, // Prevent selection of dates before today
            changeMonth: true,
            changeYear: true
        });
    }
    
    // Room filtering functionality
    $('.filter-checkbox').on('change', function() {
        var kingBed = $('#kingBed').is(':checked');
        var doubleBeds = $('#doubleBeds').is(':checked');
        var threeGuests = $('#threeGuests').is(':checked');
        var fourGuests = $('#fourGuests').is(':checked');
        
        $.ajax({
            url: '/Rooms/FilterRooms',
            type: 'POST',
            data: {
                kingBed: kingBed,
                doubleBeds: doubleBeds,
                threeGuests: threeGuests,
                fourGuests: fourGuests
            },
            success: function(result) {
                $('#roomList').html(result);
                initializeRoomButtons(); // Reinitialize event handlers for new content
            }
        });
    });
    
    // Room details modal functionality
    function initializeRoomButtons() {
        $('.details-btn').on('click', function(e) {
            e.preventDefault();
            var roomId = $(this).data('room-id');
            var roomName = $(this).data('room-name');
            var roomDesc = $(this).data('room-desc');
            var roomImg = $(this).data('room-img');
            var roomPrice = $(this).data('room-price');
            
            $('#modal-room-name').text(roomName);
            $('#modal-room-desc').text(roomDesc);
            $('#modal-room-image').attr('src', roomImg);
            $('#modal-room-price').text(roomPrice);
            
            // Set the room ID for booking
            $('#details-book-now').data('room-id', roomId);
            
            $('#detailsModal').modal('show');
        });
        
        $('.booking-btn, #details-book-now').on('click', function(e) {
            e.preventDefault();
            var roomId = $(this).data('room-id');
            
            $('#modal-room-id').val(roomId);
            $('#detailsModal').modal('hide');
            $('#bookingModal').modal('show');
        });
    }
    
    // Initialize events for room buttons
    initializeRoomButtons();
    
    // Handle the booking form submission
    $('#booking-form').on('submit', function(e) {
        // Form validation can be added here
        if (!$('#guest-name').val() || !$('#guest-email').val() || !$('#guest-phone').val()) {
            e.preventDefault();
            alert('Please fill all required fields');
            return false;
        }
        
        // If validation passes, the form will submit to the Booking controller
    });
});
