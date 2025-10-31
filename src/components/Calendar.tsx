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
} from "@/components/ui/dialog";
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Textarea } from '@/components/ui/textarea';

export default function WorkoutCalendar() {
	const [events, setEvents] = useState([
		{ title: 'Leg Day', date: '2025-10-10' },
		{ title: 'Push Day', date: '2025-10-12' },
	]);

	//dialog state
	const [isOpen, setIsOpen] = useState(false);
	const [selectedDate, setSelectedDate] = useState<string | null>(null);
	const [notes, setNotes] = useState(' ');

	const handleCreateWorkout = async () => {
		if (!selectedDate) return;
		try {
			const newWorkout = await createWorkout({ date: selectedDate, notes })
			setEvents((prev) => [
				...prev,
				{ title: newWorkout.notes || 'Workout', date: newWorkout.date },
			]);
			setIsOpen(false);
			setNotes('');
		}
		catch (err) {
			console.error('Error creating workouit:', err);
		}
	};

	return (
		<div className="p-4">
			<FullCalendar
				plugins={[dayGridPlugin, interactionPlugin]}
				initialView="dayGridMonth"
				events={events}
				height="auto"
				dateClick={(info) => {
					setSelectedDate(info.dateStr); //save clicked date
					setIsOpen(true); //open the dialog
				}}
			/>

			{/* dialog opens when isOpen = true */}
			<Dialog open={isOpen} onOpenChange={setIsOpen}>
				<DialogContent>
					<DialogHeader>
						<DialogTitle>Log workout</DialogTitle>
						<DialogDescription>
							{selectedDate ? `For $(selectedDate}` : 'Pick a date'}
						</DialogDescription>
					</DialogHeader>

					<div className='hlex flex-col gap-3 mt-2'>
						<Input
							type="date"
							value={selectedDate ?? ''}
							onChange={(e) => setSelectedDate(e.target.value)}
						/>
						<Textarea
							placeholder='Add workout notes (optional)'
							value={notes}
							onChange={(e) => setNotes(e.target.value)}
						/>
					</div>
					<DialogFooter className='mt-4'>
						{/* <Button variant="primary" onClick={() => router.push(`/workouts/${newWorkout.id}`)}> */}
						{/* 	Add */}
						{/* </Button> */}
						<Button variant="secondary" onClick={() => setIsOpen(false)}>
							{/*close the window */}
							Cancel
						</Button>
					</DialogFooter>
				</DialogContent>
			</Dialog>

		</div >
	);
}
