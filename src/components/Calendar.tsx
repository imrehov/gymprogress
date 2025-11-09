'use client';

import FullCalendar from '@fullcalendar/react';
import dayGridPlugin from '@fullcalendar/daygrid';
import interactionPlugin from '@fullcalendar/interaction';
import { useEffect, useState } from 'react';
import { createWorkout, listWorkouts } from '@/lib/api';
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
import { useRouter } from 'next/navigation';

type FcEvent = { id: string; title: string; date: string; };

export default function WorkoutCalendar() {
	const router = useRouter();

	const [events, setEvents] = useState<FcEvent[]>([]);
	const [range, setRange] = useState<{ start: string; end: string } | null>(null);

	//dialog state
	const [isOpen, setIsOpen] = useState(false);
	const [selectedDate, setSelectedDate] = useState('');
	const [notes, setNotes] = useState('');
	const [isSubmitting, setIsSubmitting] = useState(false);

	useEffect(() => {
		if (!range) return;

		(async () => {
			try {
				const workouts = await listWorkouts(range.start, range.end);
				setEvents(
					workouts.map((w) => ({
						id: w.id,
						date: w.date,
						title:
							w.notes && w.notes.trim().length > 0 ? w.notes : `workout on ${w.date}`,
					}))
				);
			} catch (e) {
				console.error('Failed to load workouts for calendar', e);
			}
		})();
	}, [range]);

	const handleCreateWorkout = async () => {
		if (!selectedDate || isSubmitting) return;
		try {
			setIsSubmitting(true);
			const newWorkout = await createWorkout({ date: selectedDate, notes })
			setEvents((prev) => [
				...prev,
				{
					id: newWorkout.id,
					date: newWorkout.date,
					title:
						newWorkout.notes && newWorkout.notes.trim().length > 0 ? newWorkout.notes : `workout on ${newWorkout.date}`,
				},
			]);
			setIsOpen(false);
			setNotes('');

			router.push(`workouts/${newWorkout.id}`);
		}
		catch (err) {
			console.error('Error creating workout:', err);
		}
		finally {
			setIsSubmitting(false);
		}
	};

	return (
		<div className="p-4">
			<FullCalendar
				plugins={[dayGridPlugin, interactionPlugin]}
				initialView="dayGridMonth"
				events={events}
				height="auto"
				datesSet={(info) => {
					const start = info.startStr.slice(0, 10);
					const end = info.endStr.slice(0, 10);
					setRange({ start, end });
				}}
				dateClick={(info) => {
					setSelectedDate(info.dateStr);
					setIsOpen(true);
				}}
				eventClick={(info) => {
					const workoutId = info.event.id;
					if (workoutId) {
						router.push(`/workouts/${workoutId}`);
					}
				}}
			/>

			{/* dialog opens when isOpen = true */}
			<Dialog open={isOpen} onOpenChange={setIsOpen}>
				<DialogContent>
					<DialogHeader>
						<DialogTitle>Log workout</DialogTitle>
						<DialogDescription>
							{selectedDate} or select another date
						</DialogDescription>
					</DialogHeader>

					<div className='flex flex-col gap-3 mt-2'>
						<Input
							type="date"
							value={selectedDate}
							onChange={(e) => setSelectedDate(e.target.value)}
						/>
						<Textarea
							placeholder='Add workout notes (optional)'
							value={notes}
							onChange={(e) => setNotes(e.target.value)}
						/>
					</div>
					<DialogFooter className='mt-4'>
						<Button onClick={handleCreateWorkout} disabled={isSubmitting}>
							{isSubmitting ? "Adding..." : "Add"}
						</Button>
						<Button variant="secondary" onClick={() => setIsOpen(false)} disabled={isSubmitting}>
							{/*close the window */}
							Cancel
						</Button>
					</DialogFooter>
				</DialogContent>
			</Dialog>

		</div >
	);
}
