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


    // ─── Helper: parse DD-MM-YYYY or YYYY-MM-DD to Date ──────────────
    function parseDate(dateStr) {
        if (!dateStr) return null;
        // Try YYYY-MM-DD first (from today's ISO)
        var parts = dateStr.split('-');
        if (parts.length === 3) {
            var year = parseInt(parts[0], 10);
            var month = parseInt(parts[1], 10) - 1;
            var day = parseInt(parts[2], 10);
            if (!isNaN(year) && !isNaN(month) && !isNaN(day)) {
                return new Date(year, month, day);
            }
        }
        // Fallback: try DD-MM-YYYY
        var parts2 = dateStr.split('-');
        if (parts2.length === 3) {
            var day2 = parseInt(parts2[0], 10);
            var month2 = parseInt(parts2[1], 10) - 1;
            var year2 = parseInt(parts2[2], 10);
            if (!isNaN(year2) && !isNaN(month2) && !isNaN(day2)) {
                return new Date(year2, month2, day2);
            }
        }
        return null;
    }



    // ── 3. History Modal Logic ──
    var historyData = [];
    var historyTableInit = false;

    function filterHistoryTable(filterValue, fromDate, toDate, searchTerm) {
        var table = $('#historyTable').DataTable();
        table.clear();
        if (!historyData || historyData.length === 0) {
            table.draw();
            $('#historyTotalCount').text(0);
            return;
        }

        var filtered = historyData.filter(function (ev) {
            var event = ev.event ? ev.event.trim() : '';
            var fleetNumber = ev.fleetNumber ? ev.fleetNumber.trim() : '';
            var location = ev.location ? ev.location.trim() : '';
            var evDate = ev.eventTimestamp ? new Date(ev.eventTimestamp) : null;

            if (filterValue !== "View All" && event !== filterValue) return false;

            // Date filtering using parseDate
            if (fromDate && evDate) {
                var from = parseDate(fromDate);
                if (from) {
                    from.setHours(0, 0, 0, 0);
                    var evStart = new Date(evDate);
                    evStart.setHours(0, 0, 0, 0);
                    if (evStart < from) return false;
                }
            }
            if (toDate && evDate) {
                var to = parseDate(toDate);
                if (to) {
                    to.setHours(23, 59, 59, 999);
                    var evEnd = new Date(evDate);
                    evEnd.setHours(23, 59, 59, 999);
                    if (evEnd > to) return false;
                }
            }

            // Search
            if (searchTerm && searchTerm.trim() !== '') {
                var term = searchTerm.trim().toLowerCase();
                var match = fleetNumber.toLowerCase().includes(term) ||
                    event.toLowerCase().includes(term) ||
                    location.toLowerCase().includes(term) ||
                    (ev.eventTimestamp ? new Date(ev.eventTimestamp).toLocaleString().toLowerCase().includes(term) : false);
                if (!match) return false;
            }

            return true;
        });

        // Sort
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

    // ─── Apply filters (always today) ──────────────────────────────
    function applyHistoryFilters() {
        var today = new Date();
        var fromDate = today.toISOString().split('T')[0]; // "YYYY-MM-DD"
        var toDate = today.toISOString().split('T')[0];
        var searchTerm = $('#historySearch').val();
        var filterValue = $('#historyFilter').val();

        console.log('applyHistoryFilters: from=' + fromDate + ', to=' + toDate); // debug
        filterHistoryTable(filterValue, fromDate, toDate, searchTerm);
    }

    // ─── Event listeners ─────────────────────────────────────────────
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
                        pageLength: 12,
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
                // Clear search and filter (no date pickers to clear)
                $('#historySearch').val('');
                $('#historyFilter').val('View All');
                applyHistoryFilters();
                $('#historyModal').modal('show');
            },
            error: function (xhr, status, error) {
                console.error('Failed to load history:', error);
                showToast('Failed to load history data', false);
            }
        });
    });

    // ─── Search input ──────────────────────────────────────────────
    $('#historySearch').on('keyup', function () {
        applyHistoryFilters();
    });

    // ─── Filter change events ──────────────────────────────────────
    $('#historyFilter').on('change', applyHistoryFilters);

    // ─── Reset on modal close ──────────────────────────────────────
    $('#historyModal').on('hidden.bs.modal', function () {
        $('#historySearch').val('');
        $('#historyFilter').val('View All');
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
        var upNext = queue.slice(bayCount, bayCount + 4);

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
        // ── Update Up Next with waiting time ──
        var upNext = queue.slice(bayCount, bayCount + 4); // adjust count
        var $upNextList = $('.up-next-list');
        $upNextList.empty();
        if (upNext.length > 0) {
            upNext.forEach(function (ev) {
                var waitingText = '';
                if (ev.event === 'Entry') {
                    var diff = new Date() - new Date(ev.eventTimestamp);
                    var minutes = Math.floor(diff / 60000);
                    var hours = Math.floor(minutes / 60);
                    var days = Math.floor(hours / 24);
                    if (days > 0) {
                        waitingText = days + 'd ' + (hours % 24) + 'h';
                    } else if (hours > 0) {
                        waitingText = hours + 'h ' + (minutes % 60) + 'm';
                    } else if (minutes > 0) {
                        waitingText = minutes + 'm';
                    } else {
                        waitingText = 'Just now';
                    }
                }
                var statusClass = ev.event === 'Entry' ? 'status-waiting' : 'status-exit';
                var statusLabel = ev.event === 'Entry' ? 'Waiting' : 'Exit';
                var waitingHtml = ev.event === 'Entry' ? '<span class="up-next-waiting"><i class="ri-time-line"></i> ' + waitingText + '</span>' : '';
                $upNextList.append(
                    '<div class="up-next-item">' +
                    '<span class="up-next-turn">#' + ev.turn + '</span>' +
                    '<span class="up-next-truck">' + ev.fleetNumber + '</span>' +
                    '<span class="up-next-status ' + statusClass + '">' + statusLabel + '</span>' +
                    waitingHtml +
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


    // ── 8. Initialize DataTable and expose refresh function ──
    initializeDataTable();

    window.refreshDashboardData = refreshDashboardData;

    refreshDashboardData();


    // ── 9. Real-Time SignalR Dashboard Refresh ──
    function registerDashboardSignalR() {

        if (!window.fleetHubConnection) {
            console.warn("⚠️ Dispatcher: fleetHubConnection not available.");
            return;
        }

        window.fleetHubConnection.on("RefreshDashboard", function () {

            console.log("📡 Dispatcher: RefreshDashboard received");

            window.refreshDashboardData();

        });

        console.log("✅ Dispatcher: RefreshDashboard handler registered");
    }


    // Register immediately if the connection already exists
    if (window.fleetHubConnection) {

        registerDashboardSignalR();

    }
    // Otherwise wait for site.js to announce that it is ready
    else {

        window.addEventListener(
            "fleetHubReady",
            registerDashboardSignalR,
            { once: true }
        );

    }

});