'use client';

import FullCalendar from '@fullcalendar/react';
import dayGridPlugin from '@fullcalendar/daygrid';
import interactionPlugin from '@fullcalendar/interaction';
import { useState } from 'react';
import { createWorkout } from '@/lib/api';
import {
	Dialog,
	DialogContent,
	DialogDescription,
	DialogHeader,
	DialogTitle,
	DialogFooter,
}import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Textarea } from '@/components/ui/textarea'; from "@/components/ui/dialog";

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
				dateClick={async (info) => {
					try {
						const newWorkout = await createWorkout({ date: info.dateStr });
						setEvents((prev) => [...prev, { title: "Workout", date: newWorkout.date }]);
					} catch (err) {
						console.error("Error creating workout:", err);
					}
				}
				}
				height="auto"
			/>
		</div>
	);
}
