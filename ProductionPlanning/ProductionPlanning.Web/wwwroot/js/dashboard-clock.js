(() => {
    const clock = document.getElementById("dashboard-clock");
    if (!clock) {
        return;
    }

    const timeZone = "Europe/Belgrade";
    const dateFormatter = new Intl.DateTimeFormat("sr-Latn-RS", {
        timeZone,
        weekday: "long",
        day: "numeric",
        month: "long",
        year: "numeric"
    });
    const timeFormatter = new Intl.DateTimeFormat("sr-Latn-RS", {
        timeZone,
        hour: "2-digit",
        minute: "2-digit",
        hourCycle: "h23"
    });
    const dateKeyFormatter = new Intl.DateTimeFormat("en-CA", {
        timeZone,
        year: "numeric",
        month: "2-digit",
        day: "2-digit"
    });
    let displayedDate = dateKeyFormatter.format(new Date());
    const updateClock = () => {
        const now = new Date();
        const currentDate = dateKeyFormatter.format(now);
        if (currentDate !== displayedDate) {
            window.location.reload();
            return;
        }

        clock.dateTime = now.toISOString();
        clock.textContent = `${dateFormatter.format(now)} | ${timeFormatter.format(now)}`;
    };

    updateClock();
    window.setInterval(updateClock, 1000);
})();
