'use client';

import FullCalendar from '@fullcalendar/react';
import dayGridPlugin from '@fullcalendar/daygrid';
import interactionPlugin from '@fullcalendar/interaction';
import { useState } from 'react';

export default function WorkoutCalendar() {
	const [events, setEvents] = useState([
		{ title: 'Leg Day', date: '2025-10-10' },
		{ title: 'Push Day', date: '2025-10-12' },
	]);

	return (
		<div className="p-4">
			<FullCalendar
				plugins={[dayGridPlugin, interactionPlugin]}
				initialView="dayGridMonth"
				events={events}
				dateClick={(info) => alert(`Clicked ${info.dateStr}`)}
				height="auto"
			/>
		</div>
	);
}
