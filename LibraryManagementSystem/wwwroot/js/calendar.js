var calendar;

document.addEventListener('DOMContentLoaded', function () {
    var calendarEl = document.getElementById('calendar');
    var categorySelect = document.getElementById('categoryFilter');

    calendar = new FullCalendar.Calendar(calendarEl, {
        schedulerLicenseKey: 'CC-Attribution-NonCommercial-NoDerivatives',
        initialView: 'resourceTimelineDay',
        headerToolbar: false,

        // Drag & Drop
        editable: true,
        eventStartEditable: false,
        eventDurationEditable: false,
        eventResourceEditable: true,

        height: '100%',
        resourceAreaWidth: '160px',
        expandRows: false, 

        slotDuration: '00:30:00',
        slotLabelInterval: '01:00',
        slotMinTime: '09:00:00',
        slotMaxTime: '22:00:00',
        nowIndicator: true,

        resources: function (info, success, failure) {
            var catId = categorySelect.value;
            fetch(calendarConfig.urls.getResources + '?categoryId=' + catId)
                .then(r => r.json()).then(d => success(d)).catch(e => failure(e));
        },
        events: function (info, success, failure) {
            var catId = categorySelect.value;
            fetch(calendarConfig.urls.getEvents + '?categoryId=' + catId)
                .then(r => r.json()).then(d => success(d)).catch(e => failure(e));
        },

        eventContent: function (arg) {
            let statusClass = arg.event.extendedProps.status === 'PENDING' ? 'bg-pending' : 'bg-confirmed';
            let title = arg.event.title;
            let time = arg.event.start.toLocaleTimeString([], { hour: 'numeric', minute: '2-digit' }) + ' - ' + arg.event.end.toLocaleTimeString([], { hour: 'numeric', minute: '2-digit' });

            return {
                html: `
                <div class="fc-event-main-content ${statusClass}">
                    <span class="evt-title">${title}</span>
                    <span class="evt-time">${time}</span>
                </div>
            `};
        },

        eventDrop: function (info) {
            var bookingId = info.event.id;
            var newRoomId = info.event.getResources()[0].id;
            var newRoomName = info.event.getResources()[0].title;


            var formData = new FormData();
            formData.append('id', bookingId);
            formData.append('newRoomId', newRoomId);

            fetch(calendarConfig.urls.moveBooking, {
                method: 'POST',
                body: formData
            })
                .then(response => response.json())
                .then(data => {
                    if (data.success) {
                        if (window.showToast) window.showToast(data.message, "success");

                        if (data.action === 'swap') {
                            calendar.refetchEvents();
                        } else {
                            info.event.setExtendedProp('roomName', newRoomName);
                        }
                    } else {
                        if (window.showToast) window.showToast(data.message, "error");
                        info.revert();
                    }
                })
                .catch(error => {
                    console.error('Error:', error);
                    if (window.showToast) window.showToast("System error, change reverted.", "error");
                    info.revert();
                });
        },

        resourceLaneClassNames: function (arg) {
            return arg.resource.extendedProps.status === 'MAINTENANCE' ? ['row-maintenance'] : [];
        },

        resourceLabelDidMount: function (info) {
            if (info.resource.extendedProps.status === 'MAINTENANCE') {
                info.el.querySelector('.fc-datagrid-cell-main').innerHTML +=
                    ' <span style="font-size:0.8em; margin-left:4px;"><svg xmlns="http://www.w3.org/2000/svg" height="24px" viewBox="0 -960 960 960" width="24px" fill="#434343"><path d="M756-120 537-339l84-84 219 219-84 84Zm-552 0-84-84 276-276-68-68-28 28-51-51v82l-28 28-121-121 28-28h82l-50-50 142-142q20-20 43-29t47-9q24 0 47 9t43 29l-92 92 50 50-28 28 68 68 90-90q-4-11-6.5-23t-2.5-24q0-59 40.5-99.5T701-841q15 0 28.5 3t27.5 9l-99 99 72 72 99-99q7 14 9.5 27.5T841-701q0 59-40.5 99.5T701-561q-12 0-24-2t-23-7L204-120Z"/></svg></span>';
            }
        },

        eventClick: function (info) { openDrawer(info.event); },
        datesSet: function (info) { document.getElementById('cal-date-title').innerText = info.view.title; }
    });

    calendar.render();

    categorySelect.addEventListener('change', function () {
        calendar.refetchResources();
        calendar.refetchEvents();
    });
});

/* =============================================================
   Drawer Logic
   ============================================================= */

function openDrawer(event) {
    var p = event.extendedProps;
    document.getElementById('dRoom').innerText = p.roomName;
    document.getElementById('dUser').innerText = p.userName;
    document.getElementById('dPrice').innerText = p.price.toFixed(2);
    document.getElementById('dBookingId').value = event.id;
    document.getElementById('dDescription').value = p.description || "";

    document.getElementById('dStart').innerText = event.start.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
    document.getElementById('dEnd').innerText = event.end.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });

    var badge = document.getElementById('dStatus');
    if (p.status === 'PENDING') {
        badge.className = 'status-pill sp-pending';
        badge.innerHTML = '<span class="dot" style="background:currentColor"></span> PENDING';
    } else if (p.status === 'MAINTENANCE') {
        badge.className = 'status-pill sp-maint';
        badge.innerHTML = 'MAINTENANCE';
    } else {
        badge.className = 'status-pill sp-confirmed';
        badge.innerHTML = '<span class="dot" style="background:currentColor"></span> CONFIRMED';
    }

    document.getElementById('details-drawer').classList.add('open');
    document.getElementById('drawer-backdrop').classList.add('active');
}

function saveDescription() {
    var id = document.getElementById('dBookingId').value;
    var desc = document.getElementById('dDescription').value;
    var btn = document.querySelector('.btn-save');

    var originalText = btn.innerText;
    btn.innerText = "Saving...";
    btn.disabled = true;
    btn.style.opacity = "0.7";

    var formData = new FormData();
    formData.append('id', id);
    formData.append('description', desc);

    fetch(calendarConfig.urls.updateDescription, {
        method: 'POST',
        body: formData
    })
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                if (window.showToast) window.showToast("Note updated successfully!", "success");

                var event = calendar.getEventById(id);
                if (event) {
                    event.setExtendedProp('description', desc);
                }
            } else {
                if (window.showToast) window.showToast("Error: " + data.message, "error");
            }
        })
        .catch(error => {
            console.error('Error:', error);
            if (window.showToast) window.showToast("Failed to connect.", "error");
        })
        .finally(() => {
            btn.innerText = "Saved ✓";
            setTimeout(() => {
                btn.innerText = originalText;
                btn.disabled = false;
                btn.style.opacity = "1";
            }, 2000);
        });
}

function closeDrawer() {
    document.getElementById('details-drawer').classList.remove('open');
    document.getElementById('drawer-backdrop').classList.remove('active');
}