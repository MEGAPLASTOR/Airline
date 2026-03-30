/**
 * Seat Selection Logic
 */

let selectedSeat = null;

function selectSeat(seatNum, status) {
    if (status === 'occupied' || status === 'blocked') return;

    if (selectedSeat) {
        let prevSeat = document.getElementById('seat-' + selectedSeat);
        if (prevSeat) {
            prevSeat.classList.remove('selected');
        }
    }

    if (selectedSeat === seatNum) {
        selectedSeat = null;
        document.getElementById('displaySeat').textContent = 'None';
        document.getElementById('hiddenSeat').value = '';
    } else {
        selectedSeat = seatNum;
        let currentSeat = document.getElementById('seat-' + seatNum);
        if (currentSeat) {
            currentSeat.classList.add('selected');
        }
        document.getElementById('displaySeat').textContent = seatNum;
        document.getElementById('hiddenSeat').value = seatNum;
    }
}
