export default function LoginForm() {
	return (
		<form className="">
			<div className="">
				<h1 className="">
					Please log in.
				</h1>
				<div className="">
					<label
						className=""
						htmlFor="email"
					>
						Email
					</label>
					<div className="relative">
						<input
							id="email"
							type="email"
							name="email"
							placeholder="Enter your email address"
							required
						/>
					</div>
				</div>
				<div className="mt-4">
					<label
						className=""
						htmlFor="password"
					>
						Password
					</label>
					<div className="relative">
						<input
							className=""
							id="password"
							type="password"
							name="password"
							placeholder="Enter password"
							required
							minLength={6}
						/>
					</div>
				</div>
			</div>
		</form >
	)
}
