/* =============================================================
   Calendar Logic
   ============================================================= */

var calendar;

document.addEventListener('DOMContentLoaded', function () {
    var calendarEl = document.getElementById('calendar');
    var categorySelect = document.getElementById('categoryFilter');

    calendar = new FullCalendar.Calendar(calendarEl, {
        schedulerLicenseKey: 'CC-Attribution-NonCommercial-NoDerivatives',
        initialView: 'resourceTimelineDay',
        headerToolbar: false,

        // 布局核心
        height: '100%',
        resourceAreaWidth: '160px',
        expandRows: false, // 关闭自动撑高，严格遵循 CSS 的 45px

        // 时间设置
        slotDuration: '00:30:00',
        slotLabelInterval: '01:00',
        slotMinTime: '09:00:00',
        slotMaxTime: '22:00:00',
        nowIndicator: true,

        // 数据加载
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

        // 事件渲染
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

        // 维护行样式
        resourceLaneClassNames: function (arg) {
            return arg.resource.extendedProps.status === 'MAINTENANCE' ? ['row-maintenance'] : [];
        },

        // 维护图标
        resourceLabelDidMount: function (info) {
            if (info.resource.extendedProps.status === 'MAINTENANCE') {
                info.el.querySelector('.fc-datagrid-cell-main').innerHTML +=
                    ' <span style="font-size:0.8em; margin-left:4px;">🔒</span>';
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
    document.getElementById('dDescription').value = p.description || ""; // 防止 null

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
                // 使用 app.js 的 toast
                if (window.showToast) window.showToast("Note updated successfully!", "success");

                // 更新日历内存
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