$(document).ready(function () {
    function generateDepartureReport() {
        const departureType = $('#departureDropdown').val();
        debugger
        $.ajax({
            url: '@Url.Action("GetDepartureReport", "ReservationReport")',
            type: 'POST',
            data: { type: departureType },
            success: function (response) {
                // Assuming `response` is an array of objects, loop through it and display it
                let html = "<table class='table'><thead><tr><th>Confirmation No</th><th>Booking Status</th><th>Reservation Date</th><th>Hotel Code</th><th>Pax Title</th><th>Pax FName</th><th>Pax LName</th><th>No Of Rooms</th><th>Room Code</th><th>Room Category Code</th><th>Room Number</th><th>Pax Email</th><th>Pax Phone Number</th><th>Pax Description</th><th>Adults</th><th>Children</th><th>Check In</th><th>Check Out</th><th>No of Days</th><th>Total Room Rate</th><th>Category</th></tr></thead><tbody>";

                response.forEach(item => {
                    html += `<tr>
                    <td>${item.ConfirmationNo}</td>
                    <td>${item.BookingStatus}</td>
                    <td>${item.ReservationDate}</td>
                    <td>${item.HotelCode}</td>
                    <td>${item.PaxTitle}</td>
                    <td>${item.PaxFName}</td>
                    <td>${item.PaxLName}</td>
                    <td>${item.NoOfRooms}</td>
                    <td>${item.RoomCode}</td>
                    <td>${item.RoomCatCode}</td>
                    <td>${item.RoomNum}</td>
                    <td>${item.PaxEmail}</td>
                    <td>${item.PaxPhoneNumber}</td>
                    <td>${item.PaxDescription}</td>
                    <td>${item.Adults}</td>
                    <td>${item.Children}</td>
                    <td>${item.CheckIn}</td>
                    <td>${item.CheckOut}</td>
                    <td>${item.NoofDay}</td>
                    <td>${item.TotalRoomRate}</td>
                    <td>${item.Category}</td>
                </tr>`;
                });

                html += "</tbody></table>";
                $('#departureReportResult').html(html);
            },
            error: function () {
                alert('Error generating departure report.');
            }
        });
    }



});