(() => {
    const dataElement = document.getElementById("product-chart-data");
    if (!dataElement || typeof Chart === "undefined") {
        return;
    }

    const planLabels = JSON.parse(dataElement.dataset.planLabels);
    const planned = JSON.parse(dataElement.dataset.planned);
    const planProduced = JSON.parse(dataElement.dataset.planProduced);
    const recordedLabels = JSON.parse(dataElement.dataset.recordLabels);
    const recorded = JSON.parse(dataElement.dataset.recorded);

    const planCanvas = document.getElementById("product-plan-chart");
    if (planCanvas) {
        new Chart(planCanvas, {
            type: "bar",
            data: {
                labels: planLabels,
                datasets: [
                    {
                        label: "Planirano",
                        data: planned,
                        backgroundColor: "rgba(52, 120, 168, 0.78)",
                        borderColor: "#3478a8",
                        borderWidth: 1,
                        borderRadius: 3
                    },
                    {
                        label: "Urađeno (prema datumu plana)",
                        data: planProduced,
                        backgroundColor: "rgba(76, 151, 114, 0.78)",
                        borderColor: "#4c9772",
                        borderWidth: 1,
                        borderRadius: 3
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
                        title: { display: true, text: "Datum plana" },
                        grid: { display: false },
                        ticks: { maxTicksLimit: 10 }
                    },
                    y: {
                        beginAtZero: true,
                        title: { display: true, text: "Količina (komada)" }
                    }
                }
            }
        });
    }

    const recordedCanvas = document.getElementById("product-recorded-chart");
    if (recordedCanvas) {
        new Chart(recordedCanvas, {
            type: "line",
            data: {
                labels: recordedLabels,
                datasets: [
                    {
                        label: "Evidentirano",
                        data: recorded,
                        borderColor: "#3478a8",
                        backgroundColor: "rgba(52, 120, 168, 0.16)",
                        borderWidth: 2,
                        pointRadius: 3,
                        pointHoverRadius: 5,
                        fill: true,
                        tension: 0.2
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
                        title: { display: true, text: "Datum evidentiranja (Europe/Belgrade)" },
                        grid: { display: false },
                        ticks: { maxTicksLimit: 10 }
                    },
                    y: {
                        beginAtZero: true,
                        title: { display: true, text: "Količina (komada)" }
                    }
                }
            }
        });
    }
})();
