/**
 * Admin Confirm Ticket Functionality
 */

function confirmPayment(bookingId) {
    return confirm(`Are you sure you want to confirm payment for Booking #BK-${bookingId}? This action cannot be undone.`);
}

document.addEventListener('DOMContentLoaded', function() {
    // Optional: Add listeners if needed for more complex UI interactions
    console.log("Admin Confirm Ticket module loaded.");
});
