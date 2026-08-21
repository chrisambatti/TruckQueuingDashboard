//-------------------------------------------------------------------
// ─── FLEET QUEUE ──────────────────────────────
//-------------------------------------------------------------------
var fleetTable = null;

$(document).ready(function () {

    // ── 1. Confirmation Modal Helper ──
    function showConfirm(message, onConfirm) {
        $('#confirmModalBody').html(message);
        $('#confirmModal').modal('show');
        $('#confirmModalOk').off('click').on('click', function () {
            $('#confirmModal').modal('hide');
            onConfirm();
        });
    }

    // ── 2. Flatpickr for History Modal ──
    if (typeof flatpickr !== 'undefined') {
        flatpickr("#historyFromDate, #historyToDate", {
            enableTime: false,
            dateFormat: "Y-m-d",
            altInput: true,
            altFormat: "d-m-Y",
            allowInput: true,
            minDate: new Date(2026, 7, 1),
            disableMobile: true,
            onChange: function () { applyHistoryFilters(); }
        });
    }

    // ── 3. History Modal Logic ──
    var historyData = [];
    var historyTableInit = false;

    function filterHistoryTable(filterValue, fromDate, toDate) {
        var table = $('#historyTable').DataTable();
        table.clear();
        if (!historyData || historyData.length === 0) {
            table.draw();
            $('#historyTotalCount').text(0);
            return;
        }
        var filtered = historyData.filter(function (ev) {
            var event = ev.event ? ev.event.trim() : '';
            var evDate = ev.eventTimestamp ? new Date(ev.eventTimestamp) : null;
            if (filterValue !== "View All" && event !== filterValue) return false;
            if (fromDate && evDate) {
                var from = new Date(fromDate);
                from.setHours(0, 0, 0, 0);
                if (evDate < from) return false;
            }
            if (toDate && evDate) {
                var to = new Date(toDate);
                to.setHours(23, 59, 59, 999);
                if (evDate > to) return false;
            }
            return true;
        });
        if (fromDate || toDate) {
            filtered.sort((a, b) => new Date(a.eventTimestamp) - new Date(b.eventTimestamp));
        } else {
            filtered.sort((a, b) => new Date(b.eventTimestamp) - new Date(a.eventTimestamp));
        }
        if (filtered.length === 0) {
            table.row.add(['<td colspan="5" class="text-center py-4 text-muted">No events found.</td>']);
        } else {
            filtered.forEach(function (ev, index) {
                var event = ev.event ? ev.event.trim() : '—';
                var eventClass = event === 'Entry' ? 'status-in' : 'status-out';
                var formattedTimestamp = ev.eventTimestamp ?
                    '<span class="datetime-date">' + new Date(ev.eventTimestamp).toLocaleDateString('en-GB', {
                        day: '2-digit', month: '2-digit', year: 'numeric'
                    }) + '</span>' +
                    '<span class="datetime-sep"> | </span>' +
                    '<span class="datetime-time">' + new Date(ev.eventTimestamp).toLocaleTimeString('en-US', {
                        hour: '2-digit', minute: '2-digit', second: '2-digit', hour12: true
                    }) + '</span>'
                    : '—';
                table.row.add([
                    index + 1,
                    ev.fleetNumber,
                    `<span class="event-badge d-inline-flex align-items-center gap-1 ${eventClass}">
                        <span class="event-dot"></span> ${event}
                    </span>`,
                    ev.location || '—',
                    formattedTimestamp
                ]);
            });
        }
        table.draw();
        $('#historyTotalCount').text(filtered.length);
    }

    function applyHistoryFilters() {
        var fromDate = $('#historyFromDate').val();
        var toDate = $('#historyToDate').val();
        if (fromDate && toDate) {
            var fromParsed = Date.parse(fromDate);
            var toParsed = Date.parse(toDate);
            if (fromParsed > toParsed) {
                showToast('From Date must be less than or equal to To Date', false);
                return;
            }
        }
        var filterValue = $('#historyFilter').val();
        filterHistoryTable(filterValue, fromDate, toDate);
    }

    $('#viewHistoryBtn').on('click', function () {
        $.ajax({
            url: '/Dashboard/GetAllEvents',
            type: 'GET',
            success: function (response) {
                if (response.error) {
                    console.error('History error:', response.error);
                    return;
                }
                historyData = response;
                if (!historyTableInit) {
                    if ($.fn.DataTable.isDataTable('#historyTable')) {
                        $('#historyTable').DataTable().destroy();
                    }
                    $('#historyTable').DataTable({
                        paging: true,
                        pageLength: 10,
                        info: true,
                        lengthChange: false,
                        searching: false,
                        order: [],
                        columnDefs: [
                            { orderable: false, targets: [0] },
                            { orderable: false, targets: [1, 2, 3] },
                            { orderable: true, targets: [4] }
                        ],
                        dom: 't<"row"<"col-sm-12 col-md-5"i><"col-sm-12 col-md-7"p>>',
                        language: {
                            info: "Showing _START_ to _END_ of _TOTAL_ events"
                        }
                    });
                    historyTableInit = true;
                }
                $('#historyFromDate').val('');
                $('#historyToDate').val('');
                $('#historyFilter').val('View All');
                applyHistoryFilters();
                $('#historyModal').modal('show');
            },
            error: function (xhr, status, error) {
            }
        });
    });

    $('#historyFilter, #historyFromDate, #historyToDate').on('change', applyHistoryFilters);
    $('#historyModal').on('hidden.bs.modal', function () {
        $('#historyFilter').val('View All');
        $('#historyFromDate').val('');
        $('#historyToDate').val('');
        applyHistoryFilters();
    });

    // ── 4. Real‑Time Dashboard Update ──
    function refreshDashboardData() {
        console.log('🔄 Refreshing dashboard data...');
        $.ajax({
            url: '/Dashboard/GetDashboardData',
            type: 'GET',
            success: function (response) {
                if (response.success) {
                    updateUI(response.data);
                } else {
                }
            },
            error: function (xhr) {
            }
        });
    }

    function initializeDataTable() {
        if ($.fn.DataTable.isDataTable('#fleetTable')) {
            fleetTable = $('#fleetTable').DataTable();
        } else {
            fleetTable = $('#fleetTable').DataTable({
                responsive: true,
                paging: true,
                pageLength: 10,
                info: true,
                lengthChange: false,
                searching: true,
                order: [[5, 'asc']],
                columnDefs: [
                    { orderable: false, targets: [0, 1, 2, 3, 4] },
                    { orderable: true, targets: [5] },
                    { targets: [5], visible: false }
                ],
                drawCallback: function () {
                    recalculateTurnNumbers();
                }
            });
        }
    }

    function updateUI(data) {
        var queue = data.fleetEvents || [];
        var bayCount = 2;

        // ── Update bays ──
        var bayTrucks = queue.slice(0, bayCount);
        var upNext = queue.slice(bayCount, bayCount + 2);

        $('.queue-stats .now-serving-ticket.primary').each(function (index) {
            var bay = bayTrucks[index];
            var $bay = $(this);
            if (bay) {
                $bay.find('.serving-number').html(
                    bay.fleetNumber + ' <small class="serving-turn">Turn ' + bay.turn + '</small>'
                );
                if (bay.calledNow) {
                    $bay.addClass('called-now');
                    $bay.find('.serving-label').html(
                        '<i class="ri-star-fill me-1"></i> Bay ' + (index + 1) + ' <span class="called-badge">★ Called</span>'
                    );
                } else {
                    $bay.removeClass('called-now');
                    $bay.find('.serving-label').text('Bay ' + (index + 1));
                }
            } else {
                $bay.find('.serving-number').text('—');
                $bay.removeClass('called-now');
                $bay.find('.serving-label').text('Bay ' + (index + 1));
            }
        });

        // ── Update Up Next ──
        var $upNextList = $('.up-next-list');
        $upNextList.empty();
        if (upNext.length > 0) {
            upNext.forEach(function (ev) {
                $upNextList.append(
                    '<div class="up-next-item">' +
                    '<span class="up-next-turn">#' + ev.turn + '</span>' +
                    '<span class="up-next-truck">' + ev.fleetNumber + '</span>' +
                    '<span class="up-next-badge badge-entry">' + ev.event + '</span>' +
                    '</div>'
                );
            });
        } else {
            $upNextList.append('<div class="up-next-item empty">— No more trucks —</div>');
        }

        // ── Update waiting count ──
        $('#waitingCount').text(queue.length);

        // ── Update waiting table ──
        if (!fleetTable) {
            initializeDataTable();
        }

        fleetTable.clear();

        if (queue.length === 0) {
            fleetTable.row.add(['<td colspan="6" class="text-center py-4 text-muted">No trucks in queue.</td>']);
        } else {
            queue.forEach(function (ev) {
                var turnBadgeClass = ev.calledNow ? 'called-now-badge' : '';
                var calledNowBadge = ev.calledNow ? '<span class="called-now-label">★ #' + ev.calledNowOrder + '</span>' : '';
                var actionHtml = ev.calledNow
                    ? '<button class="btn-revert" data-vehicle="' + ev.fleetNumber + '" title="Revert Call Now"><i class="ri-arrow-go-back-line"></i></button>'
                    : '<button class="btn-callnow" data-vehicle="' + ev.fleetNumber + '" title="Call Now"><i class="ri-arrow-up-double-line"></i></button>';

                var rowData = [
                    '<span class="turn-badge-clinic ' + turnBadgeClass + '">' + ev.turn + '</span>',
                    '<span class="fleet-number">' + ev.fleetNumber + '</span> ' + calledNowBadge,
                    '<span class="status-badge ' + (ev.event === "Entry" ? "status-in" : "status-out") + '">' + ev.event + '</span>',
                    '<span class="datetime-date">' + new Date(ev.eventTimestamp).toLocaleDateString('en-GB') + '</span>' +
                    '<span class="datetime-sep"> | </span>' +
                    '<span class="datetime-time">' + new Date(ev.eventTimestamp).toLocaleTimeString('en-US', { hour: '2-digit', minute: '2-digit', second: '2-digit', hour12: true }) + '</span>',
                    actionHtml,
                    ev.calledNow ? '0' : ev.turn   
                ];
                fleetTable.row.add(rowData);
            });
        }
         
        fleetTable.draw(); 
    }

    function recalculateTurnNumbers() {
        var table = $('#fleetTable').DataTable();
        var rows = table.rows({ search: 'applied' }).nodes();
        $(rows).each(function (index, row) {
            var turnCell = $(row).find('td:first-child');
            turnCell.html('<span class="turn-badge-clinic">' + (index + 1) + '</span>');
        });
    }

    // ── 5. Call Now Button ──
    $(document).on('click', '.btn-callnow', function (e) {
        e.stopPropagation();
        var $btn = $(this);
        var vehicle = $btn.data('vehicle');

        if (!vehicle) return;

        var message = 'Are you sure you want to move truck <strong>' + vehicle + '</strong> to the front of the queue?';
        showConfirm(message, function () {
            $btn.prop('disabled', true).html('<i class="ri-loader-4-line ri-spin"></i>');
            $.ajax({
                url: '/Dashboard/CallNow',
                type: 'POST',
                data: { vehicleNumber: vehicle },
                success: function (response) {
                    if (response.success) {
                        refreshDashboardData();
                        $btn.prop('disabled', false).html('<i class="ri-arrow-up-double-line"></i>');
                    } else {
                        showToast(response.message, false);
                        $btn.prop('disabled', false).html('<i class="ri-arrow-up-double-line"></i>');
                    }
                },
                error: function (xhr, status, error) {
                    showToast('Error calling truck: ' + error, false);
                    $btn.prop('disabled', false).html('<i class="ri-arrow-up-double-line"></i>');
                }
            });
        });
    });

    // ── 6. Revert Button ──
    $(document).on('click', '.btn-revert', function (e) {
        e.stopPropagation();
        var $btn = $(this);
        var vehicle = $btn.data('vehicle');

        if (!vehicle) return;

        var message = 'Are you sure you want to undo <strong>Call Now</strong> for truck <strong>' + vehicle + '</strong>?';
        showConfirm(message, function () {
            $btn.prop('disabled', true).html('<i class="ri-loader-4-line ri-spin"></i>');
            $.ajax({
                url: '/Dashboard/RevertCallNow',
                type: 'POST',
                data: { vehicleNumber: vehicle },
                success: function (response) {
                    if (response.success) {
                        refreshDashboardData();
                        $btn.prop('disabled', false).html('<i class="ri-arrow-go-back-line"></i>');
                    } else {
                        showToast(response.message, false);
                        $btn.prop('disabled', false).html('<i class="ri-arrow-go-back-line"></i>');
                    }
                },
                error: function (xhr, status, error) {
                    showToast('Error reverting: ' + error, false);
                    $btn.prop('disabled', false).html('<i class="ri-arrow-go-back-line"></i>');
                }
            });
        });
    });

    // ── 8. Initialize DataTable and load initial data ──
    initializeDataTable();
    refreshDashboardData();   


    window.refreshDashboardData = refreshDashboardData;
});