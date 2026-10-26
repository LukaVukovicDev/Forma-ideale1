(() => {
    const dataElement = document.getElementById("machine-chart-data");
    if (!dataElement || typeof Chart === "undefined") {
        return;
    }

    const labels = JSON.parse(dataElement.dataset.labels);
    const planned = JSON.parse(dataElement.dataset.planned);
    const produced = JSON.parse(dataElement.dataset.produced);
    const downtime = JSON.parse(dataElement.dataset.downtime);

    const productionCanvas = document.getElementById("production-chart");
    if (productionCanvas) {
        new Chart(productionCanvas, {
            type: "bar",
            data: {
                labels,
                datasets: [
                    {
                        label: "Planirano",
                        data: planned,
                        backgroundColor: "rgba(52, 120, 168, 0.78)",
                        borderColor: "#3478a8",
                        borderWidth: 1,
                        borderRadius: 4
                    },
                    {
                        label: "Urađeno",
                        data: produced,
                        backgroundColor: "rgba(76, 151, 114, 0.78)",
                        borderColor: "#4c9772",
                        borderWidth: 1,
                        borderRadius: 4
                    }
                ]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                interaction: { mode: "index", intersect: false },
                plugins: {
                    legend: { position: "bottom" },
                    tooltip: { enabled: true }
                },
                scales: {
                    x: {
                        title: { display: true, text: "Datum" },
                        grid: { display: false }
                    },
                    y: {
                        beginAtZero: true,
                        title: { display: true, text: "Količina (komada)" }
                    }
                }
            }
        });
    }

    const downtimeCanvas = document.getElementById("downtime-chart");
    if (downtimeCanvas) {
        new Chart(downtimeCanvas, {
            type: "bar",
            data: {
                labels,
                datasets: [
                    {
                        label: "Zastoj",
                        data: downtime,
                        backgroundColor: "rgba(213, 147, 67, 0.8)",
                        borderColor: "#d59343",
                        borderWidth: 1,
                        borderRadius: 4
                    }
                ]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: { position: "bottom" },
                    tooltip: { enabled: true }
                },
                scales: {
                    x: {
                        title: { display: true, text: "Datum" },
                        grid: { display: false }
                    },
                    y: {
                        beginAtZero: true,
                        title: { display: true, text: "Vreme zastoja (minuti)" }
                    }
                }
            }
        });
    }
})();
