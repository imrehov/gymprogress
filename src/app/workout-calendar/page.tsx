import { redirect } from 'next/navigation';
import Calendar from '@/components/Calendar';

//auth check

export default async function WorkoutsPage() {
	const r = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/v1/auth/me`, {
		cache: 'no-store',
		credentials: 'include',
	});

	if (r.status === 401) {
		redirect('/login');
	}


	return (
		<main className="min-h-screen p-8">
			<h1 className="text-2xl font-bold mb-4">Workout Calendar</h1>
			<Calendar />
		</main>
	);
}
