$(document).ready(function () {
    if ($(window).width() > 700) {
        $("#MobileSearch").remove();
        $("#WindowSearch").show();
    }
    else {
        $("#MobileSearch").show();
        $("#WindowSearch").remove();
    }
});
function check() {
    $('#myModal').modal('show');
}

function close() {
    $('#myModal').modal('hide');
}
function ShowInputContainer() {
    $('#InputOverlay').show();
    $('#InputContainer').show();
}
function HideInputContainer() {
    $('#InputOverlay').hide();
    $('#InputContainer').hide();
}
function ShowDatepickerContainer() {
    $('#DatepickerOverlay').show();
    $('#DatepickerContainer').show();
    $('html, body').css({
        overflow: 'hidden',
        height: '100%'
    });
    $('#MobileDaterangePicker').click()

}
function HideDatepickerContainer() {
    $('#DatepickerOverlay').hide();
    $('#DatepickerContainer').hide();
    $('html, body').css({
        overflow: 'auto',
        height: 'auto'
    });
}

function showCategory(category) {
    // Hide all categories first
    document.getElementById('Naran').classList.add('hidden');
    document.getElementById('Islamabad').classList.add('hidden');
    document.getElementById('Skardu').classList.add('hidden');

    // Show the selected category
    if (category === 'M') {
        document.getElementById('Naran').classList.remove('hidden');
    } else if (category === 'I') {
        document.getElementById('Islamabad').classList.remove('hidden');
    } else if (category === 'K') {
        document.getElementById('Skardu').classList.remove('hidden');
    }
}

$(document).ready(function () {
    // By default, display Naran section and set the Naran button as active
    showCategory('M');

    // Add a click event for category-specific filter logic
    $('.filtr-button.filtr').on('click', function () {
        var category = $(this).data('filter');
        showCategory(category);
    });
});
$(document).ready(function () {
    // Ensure "All" is selected initially
    $('[data-filter="all"]').addClass('active');

    // Initialize the filter category to show Naran
    showCategory('M');

    // Add click event for filter buttons
    $('.filtr-button.filtr').on('click', function () {
        var category = $(this).data('filter');  // Use the data-filter attribute
        showCategory(category);  // Call function to show the corresponding category
        $('.filtr-button').removeClass('active');  // Remove active class from all buttons
        $(this).addClass('active');  // Add active class to the clicked button
    });
});


$(document).ready(function () {
    $('.property-Naran').on('click', function (event) {
        event.preventDefault();
        $('#propertyModal2').modal('show');
    });
});
$(document).ready(function () {
    $('.property-Naran-1').on('click', function (event) {
        event.preventDefault();
        $('#propertyModal2-1').modal('show');
    });
});
$(document).ready(function () {
    $('.property-Naran-2').on('click', function (event) {
        event.preventDefault();
        $('#propertyModal2-2').modal('show');
    });
});

$(document).ready(function () {
    $('.property-video').on('click', function (event) {
        event.preventDefault();
        $('#propertyModal').modal('show');
    });
});
$(document).ready(function () {
    $('.property-video-1').on('click', function (event) {
        event.preventDefault();
        $('#propertyModal-1').modal('show');
    });
})
$(document).ready(function () {
    $('.property-video-2').on('click', function (event) {
        event.preventDefault();
        $('#propertyModal-2').modal('show');
    });
})
$(document).ready(function () {
    $('.property-video-3').on('click', function (event) {
        event.preventDefault();
        $('#propertyModal-3').modal('show');
    });
})
$(document).ready(function () {
    $('.property-video-4').on('click', function (event) {
        event.preventDefault();
        $('#propertyModal-4').modal('show');
    });
})

$(document).ready(function () {
    $('.property-video3').on('click', function (event) {
        event.preventDefault();
        $('#propertyModal3-1').modal('show');
    });
})


function toggleView(elementId) {
    var element = document.getElementById(elementId);
    if (element.style.display === "none") {
        element.style.display = "block";
    } else {
        element.style.display = "none";
    }
}

function selectLocation(id, location) {
    // Update the hidden input with the selected location ID
    document.getElementById('PropertyId').value = id;

    // Update the label with the selected location
    document.getElementById('PropertyIdLabel').innerText = location;

    // Hide the location popup
    toggleView('Locations');
}

// Ensure that the popup is initially hidden on page load
window.onload = function () {
    document.getElementById('Locations').style.display = 'none';
};
var StartDate = new Date();
var EndDate = new Date();
var ChildValue = 0;
var AdultValue = 1;
var RoomValue = 1;
var PropertyId = 1;


function CalculatePlusAdult() {
    var value = parseInt($("#AdultValue").html());
    value = value + 1;
    $("#AdultValue").html(value);
    $("#AdultValueUpper").html(value + "&nbsp adults");
    $('.adults-select').val(value);
    AdultValue = value;
}
function CalculateMinusAdult() {
    var value = parseInt($("#AdultValue").html());
    value = value - 1;
    if (value <= 0) {
        return;
    }
    $("#AdultValue").html(value);
    $("#AdultValueUpper").html(value + "&nbsp adults");
    $('.adults-select').val(value);
    AdultValue = value;
}
function CalculatePlusChildren() {
    var value = parseInt($("#ChildValue").html());
    value = value + 1;
    $("#ChildValue").html(value);
    $("#ChildrenValueUpper").html(value + "&nbsp children");
    $('.children-select').val(value);
    ChildValue = value;
    ShowHideChildSelect(value);
}
function CalculateMinusChildren() {
    var value = parseInt($("#ChildValue").html());
    value = value - 1;
    if (value < 0) {
        return;
    }
    $("#ChildValue").html(value);
    $("#ChildrenValueUpper").html(value + "&nbsp children");
    $('.children-select').val(value);
    ChildValue = value;
    ShowHideChildSelect(value);
}
function CalculatePlusRooms() {
    var value = parseInt($("#RoomValue").html());
    value = value + 1;
    $("#RoomValue").html(value);
    $("#RoomValueUpper").html(value + "&nbsp rooms");
    $('.room-select').val(value);
    RoomValue = value

}
function CalculateMinusRooms() {
    var value = parseInt($("#RoomValue").html());
    value = value - 1;
    if (value <= 0) {
        return;
    }
    $("#RoomValue").html(value);
    $("#RoomValueUpper").html(value + "&nbsp rooms");
    $('.room-select').val(value);
    RoomValue = value
}
function ShowHideChildSelect(Customvalue) {
    if (Customvalue == 0) {
        $("#AgeEntryContainer").hide();
        $("#AgeEntryContainer2").hide();
        $("#AgeEntryContainer3").hide();
        $("#AgeEntryContainer4").hide();
    }
    else if (Customvalue == 1) {
        $("#AgeEntryContainer").show();
        $("#AgeEntryContainer2").hide();
        $("#AgeEntryContainer3").hide();
        $("#AgeEntryContainer4").hide();
    }
    else if (Customvalue == 2) {
        $("#AgeEntryContainer").show();
        $("#AgeEntryContainer2").show();
        $("#AgeEntryContainer3").hide();
        $("#AgeEntryContainer4").hide();
    }
    else if (Customvalue == 3) {
        $("#AgeEntryContainer").show();
        $("#AgeEntryContainer2").show();
        $("#AgeEntryContainer3").show();
        $("#AgeEntryContainer4").hide();
    }
    else if (Customvalue == 4) {
        $("#AgeEntryContainer").show();
        $("#AgeEntryContainer2").show();
        $("#AgeEntryContainer3").show();
        $("#AgeEntryContainer4").show();
    }
}
$(document).ready(function () {

    if ($(window).width() < 700) {
        // Default: Today as check-in and Tomorrow as checkout
        var today = moment();
        var tomorrow = moment().add(1, 'days');

        $('#MobileDaterangePicker').daterangepicker({
            opens: 'center',
            parentEl: '#DatepickerParentEl',
            alwaysShowCalendars: true,
            autoUpdateInput: false, // Prevent default text update
            locale: {
                format: 'MM/DD/YYYY', // Force backend format
                separator: " to"
            },
            minDate: today,
            startDate: today,     // Check-in = Today
            endDate: tomorrow,    // Checkout = Tomorrow
            singleDatePicker: false,
            showCustomRangeLabel: false
        }, function (start, end) {
            HideDatepickerContainer();
            StartDate = moment(start).format('MM/DD/YYYY');
            EndDate = moment(end).format('MM/DD/YYYY');

            console.log("[Callback] Check-in:", StartDate, "| Check-out:", EndDate);

            // Frontend show in long format, backend keep MM/DD/YYYY
            $('#ci_date_text').text(moment(start).format('ddd, MMM DD, YYYY'));
            $('#co_date_text').text(moment(end).format('ddd, MMM DD, YYYY'));

            // Also update input field for backend
            $('#MobileDaterangePicker').val(StartDate + ' to ' + EndDate);
        });

        // --- Ensure default today/tomorrow set on page load ---
        StartDate = today.format('MM/DD/YYYY');
        EndDate = tomorrow.format('MM/DD/YYYY');
        console.log("[On Load] Check-in:", StartDate, "| Check-out:", EndDate);
        $('#ci_date_text').text(today.format('ddd, MMM DD, YYYY'));
        $('#co_date_text').text(tomorrow.format('ddd, MMM DD, YYYY'));
        $('#MobileDaterangePicker').val(StartDate + ' to ' + EndDate);

        $('#MobileDaterangePicker').on('apply.daterangepicker', function (ev, picker) {
            var selectedStartDate = picker.startDate.format('MM/DD/YYYY');
            var selectedEndDate = picker.endDate.format('MM/DD/YYYY');

            // If both are same, force next day as checkout
            if (selectedStartDate === selectedEndDate) {
                picker.setEndDate(picker.startDate.clone().add(1, 'days'));
            }

            StartDate = picker.startDate.format('MM/DD/YYYY');
            EndDate = picker.endDate.format('MM/DD/YYYY');

            console.log("[Apply] Check-in:", StartDate, "| Check-out:", EndDate);

            // Update input manually to prevent default long format
            $('#MobileDaterangePicker').val(StartDate + ' to ' + EndDate);

            // Frontend show in long format
            $('#ci_date_text').text(picker.startDate.format('ddd, MMM DD, YYYY'));
            $('#co_date_text').text(picker.endDate.format('ddd, MMM DD, YYYY'));
        });

    }


    else {
        $('#DesktopDaterangePicker').daterangepicker({
            opens: 'left',
            locale: {
                format: 'DD MMM YYYY',
                separator: " to"
            },
            "autoApply": true,
            autoUpdateInput: false,
            minDate: new Date(),
            endDate: new Date(),
            singleDatePicker: false, // Allow multiple date selections
            showCustomRangeLabel: false
        }, function (start, end, label) {
            // Handle the date selection logic
            StartDate = moment(start).format('MM/DD/YYYY');
            EndDate = moment(end).format('MM/DD/YYYY');
        });

        // Automatically update the selected date range and prevent multiple selections
        $('#DesktopDaterangePicker').on('apply.daterangepicker', function (ev, picker) {
            var selectedStartDate = picker.startDate.format('YYYY/MM/DD');
            var selectedEndDate = picker.endDate.format('YYYY/MM/DD');

            // If the selected dates are the same, select both the selected day and the next day
            if (selectedStartDate === selectedEndDate) {
                picker.setEndDate(picker.startDate.add(1, 'days')); // Automatically select the next day
                picker.setStartDate(picker.startDate.subtract(1, 'days')); // Keep the original start date as selected
            }

            // Update the input with the selected date range
            $(this).val(picker.startDate.format('MM/DD/YYYY') + ' - ' + picker.endDate.format('MM/DD/YYYY'));
        });

        $('#DesktopDaterangePicker').on('cancel.daterangepicker', function (ev, picker) {
            $(this).val('');
        });
    }
});

function SearchResults() {
    if ($("#DesktopDaterangePicker").val() == "") {
        alert("Please select Check-in and Check-out Date");
        return;
    }

    if ($("#PropertyId").val() == "-1") {
        alert("Please Select Your Desired City");
        return;
    }

    // Ensure that if CheckIn and CheckOut are the same date, we add 1 day to CheckOut
    if (StartDate === EndDate) {
        var startDateMoment = moment(StartDate, 'MM/DD/YYYY');
        EndDate = startDateMoment.add(1, 'days').format('MM/DD/YYYY');
    }

    // Redirect with the correct CheckIn and CheckOut dates
    window.location.href = "/Home/PropertySearch?CheckIn=" + StartDate + "&CheckOut=" + EndDate + "&Adults=" + AdultValue + "&PropertyId=" + $("#PropertyId").val() + "&Children=" + ChildValue + "&NoOfRooms=" + RoomValue;
}

function ToggleView(InputContainer) {
    var e = document.getElementById(InputContainer);
    if (e.style.display == 'block') {
        e.style.display = 'none';
    }
    else {
        e.style.display = 'block';

    }
}
function CheckDisplay() {
    if ($("#DatepickerContainer").style.display == "block") {
        document.body.style.overflow = "hidden";
    }
    else {
        document.body.style.overflow = "scroll";

    }
}