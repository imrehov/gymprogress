'use client';

import { useParams } from 'next/navigation';

export default function WorkoutPage() {
	const { id } = useParams();

	return (
		<main className="p-8">
			<h1 className='text-2x1 font-bold'>Workout details</h1>
			<p>ID: {id} </p>
			<p>TODO: add editor and sets and exercises </p>
		</main >
	);
}
