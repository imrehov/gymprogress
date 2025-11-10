import Link from 'next/link';

export default function HomePage() {
	return (
		<main className="flex flex-col items-center justify-center min-h-screen p-8 space-y-6 bg-gray-50">
			<h1 className="text-4xl font-bold">Gym Progress Tracker</h1>
			<p className="text-gray-600">Track your workouts, visualize your gains.</p>

			<div className="flex gap-4 mt-8">
				<Link
					href="/login"
					className="px-20 py-20 bg-red-600 text-white rounded-lg hover:bg-red-700 transition"
				>
					Login
				</Link>

				<Link
					href="/register"
					className="px-20 py-20 border border-red-600 text-red-600 rounded-lg hover:bg-red-50 transition"
				>
					Register
				</Link>
			</div>
		</main>
	);
}
