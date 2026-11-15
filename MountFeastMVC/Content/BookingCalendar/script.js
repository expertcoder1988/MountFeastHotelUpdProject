$(document).ready(function () {
    const propertyID = 1; // Assuming propertyID is 1 for now, modify as needed
    const startDate = new Date(new Date().getFullYear(), 3, 1); // April 1st
    const endDate = new Date(new Date().getFullYear(), 10, 30); // November 30th
    let currentDate = new Date(); // Today

    // Ensure currentDate is within the valid range
    if (currentDate < startDate) currentDate = startDate;
    if (currentDate > endDate) currentDate = endDate;

    let roomCategoriesData = null; // To store the fetched room categories data

    function loadCalendar() {
        $.ajax({
            url: '/Calander/GetEventsNew',
            method: 'GET',
            data: { propertyID: propertyID },
            success: function (data) {
                console.log("Data received from server:", data);
                roomCategoriesData = data; // Store the data
                generateCalendar(roomCategoriesData, currentDate);
            },
            error: function (error) {
                console.error("Error fetching bookings: ", error);
            }
        });
    }

    function parseDate(jsonDate) {
        // Parse the JSON date string and return a Date object without changing timezone
        return new Date(parseInt(jsonDate.replace('/Date(', '').replace(')/', ''), 10));
    }

    function normalizeDate(date) {
        return new Date(date.getFullYear(), date.getMonth(), date.getDate());
    }

    function generateCalendar(roomCategories, startDate) {
        if (!roomCategories || !Array.isArray(roomCategories)) {
            console.error("Invalid roomCategories data:", roomCategories);
            return;
        }

        const dates = [];
        const viewStartDate = new Date(startDate); // Start from current date
        const viewEndDate = new Date(viewStartDate);
        viewEndDate.setDate(viewEndDate.getDate() + 19); // Show 20 days initially

        for (let d = new Date(viewStartDate); d <= viewEndDate; d.setDate(d.getDate() + 1)) {
            dates.push(new Date(d));
        }

        const roomCategoriesTable = $('#roomCategoriesTable tbody');
        const bookingCalendarTable = $('#bookingCalendar tbody');
        const thead = $('#bookingCalendar thead tr');

        roomCategoriesTable.empty(); // Clear existing room categories
        thead.empty(); // Clear existing header
        bookingCalendarTable.empty(); // Clear existing body

        // Add dates to the header
        thead.append('<th></th>'); // Add an empty header cell to align with the room categories table
        dates.forEach(date => {
            const th = $('<th></th>').text(date.toDateString().slice(4, 10));
            thead.append(th);
        });

        // Add room categories and their bookings to the body
        roomCategories.forEach(category => {
            if (!category || !category.Category || !Array.isArray(category.Rooms)) {
                console.error("Invalid category data:", category);
                return;
            }

            // Add category row in the room categories table
            const categoryRow = $('<tr></tr>').addClass('category-row');
            const categoryTd = $('<td></td>')
                .text(category.Category)
                .attr('colspan', 1);
            categoryRow.append(categoryTd);
            roomCategoriesTable.append(categoryRow);

            // Add empty row in the bookings table to align with the category row
            const emptyRow = $('<tr></tr>').addClass('category-row');
            const emptyTd = $('<td></td>').attr('colspan', dates.length + 1);
            emptyRow.append(emptyTd);
            bookingCalendarTable.append(emptyRow);

            // Add room number and bookings rows
            category.Rooms.forEach(room => {
                const roomRow = $('<tr></tr>');
                const roomTd = $('<td></td>').text(room.RoomNum);
                roomRow.append(roomTd);
                roomCategoriesTable.append(roomRow);

                const tr = $('<tr></tr>'); // Do not add room number in the booking calendar table
                dates.forEach(date => {
                    const booking = room.Bookings.find(b => {
                        const checkIn = normalizeDate(parseDate(b.CheckIn));
                        const checkOut = normalizeDate(parseDate(b.CheckOut));
                        // Ensure both checkIn and checkOut are inclusive
                        console.log(`Date: ${date.toISOString().slice(0, 10)}, CheckIn: ${checkIn.toISOString().slice(0, 10)}, CheckOut: ${checkOut.toISOString().slice(0, 10)}`);
                        return checkIn <= date && date <= checkOut;
                    });
                    const status = booking ? "booked" : "available";
                    const td = $('<td></td>').addClass(`${status}-td`);
                    tr.append(td);
                });
                bookingCalendarTable.append(tr);
            });
        });

        // Scroll to today's date
        const firstVisibleDateIndex = dates.findIndex(date => {
            const today = currentDate.toDateString();
            return date.toDateString() === today;
        });
        const scrollAmount = firstVisibleDateIndex * 100; // Adjust the value as per your need
        setTimeout(() => {
            $('.scrollable-table').scrollLeft(scrollAmount);
        }, 0);
    }

    function updateCalendarHeader() {
        $('#calendarMonthYear').text(currentDate.toLocaleString('default', { month: 'long', year: 'numeric' }));
    }

    $('#prevButton').click(function () {
        currentDate.setDate(currentDate.getDate() - 20);
        if (currentDate < startDate) currentDate = startDate; // Prevent going before start date
        generateCalendar(roomCategoriesData, currentDate); // Update the calendar without reloading from server
        updateCalendarHeader();
    });

    $('#nextButton').click(function () {
        currentDate.setDate(currentDate.getDate() + 20);
        if (currentDate > endDate) currentDate = endDate; // Prevent going past end date
        generateCalendar(roomCategoriesData, currentDate); // Update the calendar without reloading from server
        updateCalendarHeader();
    });

    loadCalendar();
    updateCalendarHeader();
});
