'use client';

import React, { useState } from "react";
import { useRouter } from 'next/navigation';
import { Button } from "@/components/ui/button";

export default function LoginPage() {
	const router = useRouter();
	const [email, setEmail] = useState('');
	const [password, setPassword] = useState('');
	const [error, setError] = useState<string | null>(null);
	const [busy, setBusy] = useState(false);

	async function handleSubmit(e: React.FormEvent) {
		e.preventDefault();
		setBusy(true);
		setError(null);

		try {
			const r = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/v1/auth/login`, {
				method: 'POST',
				headers: { 'Content-Type': 'application/json' },
				credentials: 'include',
				body: JSON.stringify({ email, password }),
			});

			if (!r.ok) {
				setError('Invalid email or password');
				return;
			}
			router.push('/workout-calendar');
		} finally {
			setBusy(false);
		}
	}

	function registerInstead() {
		router.push('/register');
	}

	return (
		<main className="p-8 max-w-md mx-auto">
			<h1 className="text-2xl font-bold mb-4">Log in</h1>
			<form className="space-y-4" onSubmit={handleSubmit}>
				<input
					className="border rounded px-2 py-1 w-full"
					type="email"
					placeholder="you@example.com"
					value={email}
					onChange={e => setEmail(e.target.value)}
				/>
				<input
					className="border rounded px-2 py-1 w-full"
					type="password"
					placeholder="password"
					value={password}
					onChange={e => setPassword(e.target.value)}
				/>
				{error && <p className="text-sm text-red-500">{error}</p>}
				<button
					type="submit"
					disabled={busy}
					className="bg-blue-600 text-white px-4 py-2 rounded"
				>
					{busy ? 'Logging in…' : 'Log in'}
				</button>
				<Button className="flex"
					variant={"secondary"}
					onClick={registerInstead}
				>
					Register instead
				</Button>
			</form>
		</main>
	);
}
