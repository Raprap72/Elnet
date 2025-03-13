document.addEventListener("DOMContentLoaded", function () {
    const filters = {
        kingBed: document.getElementById("kingBed"),
        doubleBeds: document.getElementById("doubleBeds"),
        threeGuests: document.getElementById("threeGuests"),
        fourGuests: document.getElementById("fourGuests")
    };

    const rooms = [
        { id: 1, element: document.querySelector(".select-book .rooms .room:nth-child(1)"), features: ["kingBed", "threeGuests"] },
        { id: 2, element: document.querySelector(".select-book .rooms .room:nth-child(2)"), features: ["doubleBeds", "fourGuests"] },
        { id: 3, element: document.querySelector(".select-book .rooms .room:nth-child(3)"), features: ["kingBed", "doubleBeds", "fourGuests"] }
    ];

    function filterRooms() {
        let activeFilters = Object.keys(filters).filter(key => filters[key].checked);
        
        rooms.forEach(room => {
            if (activeFilters.length === 0 || activeFilters.some(filter => room.features.includes(filter))) {
                room.element.style.display = "flex";
            } else {
                room.element.style.display = "none";
            }
        });
    }

    Object.values(filters).forEach(filter => filter.addEventListener("change", filterRooms));
});
