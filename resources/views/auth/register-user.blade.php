<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="UTF-8">
<meta name="viewport" content="width=device-width, initial-scale=1.0">
<title>KDTMS | Register User</title>
<script src="https://cdn.tailwindcss.com"></script>
<style>
    .floating-input { position: relative; }
    .floating-input input {
        width: 100%; padding: 1rem 0.75rem 0.25rem 0.75rem;
        border: 1px solid #d1d5db; border-radius: 0.5rem;
        outline: none; transition: border-color 0.3s;
    }
    .floating-input label {
        position: absolute; top: 1rem; left: 0.75rem;
        color: #6b7280; font-size: 0.875rem;
        pointer-events: none; transition: all 0.2s ease-out;
        background: white; padding: 0 0.25rem;
    }
    .floating-input input:focus + label,
    .floating-input input:not(:placeholder-shown) + label {
        top: -0.5rem; left: 0.5rem;
        font-size: 0.75rem; color: #f59e0b;
    }
</style>
</head>
<body class="bg-gray-100 flex items-center justify-center min-h-screen">

<div class="bg-white shadow-lg rounded-xl w-full max-w-sm p-8">
   <div class="flex justify-center mb-6">
        <img src="{{ asset('img/logo.jpg') }}" class="h-12" style="width: 220px; height: auto;">
    </div>

    <h2 class="text-2xl font-bold text-center text-gray-800 mb-6">Register as User</h2>

    @if ($errors->any())
        <div class="bg-red-100 text-red-700 px-4 py-2 rounded mb-4 text-sm">
            {{ $errors->first() }}
        </div>
    @endif

    <form method="POST" action="{{ route('register.user.submit') }}" class="space-y-5">
        @csrf

        <div class="floating-input">
            <input type="text" name="name" placeholder=" " required>
            <label>Name</label>
        </div>

        <div class="floating-input">
            <input type="email" name="email" placeholder=" " required>
            <label>Email</label>
        </div>

        <div class="floating-input">
            <input type="password" name="password" placeholder=" " required>
            <label>Password</label>
        </div>

        <div class="floating-input">
            <input type="password" name="password_confirmation" placeholder=" " required>
            <label>Confirm Password</label>
        </div>

        <button type="submit" class="w-full py-3 bg-blue-500 hover:bg-blue-600 text-white font-semibold rounded-lg shadow-sm mt-4">
            Register
        </button>
    </form>

    <p class="text-center text-sm text-gray-500 mt-5">
        Already have an account? 
        <a href="{{ route('login') }}" class="text-amber-600 font-semibold hover:underline">Login here</a>
    </p>

     <p class="text-center text-gray-400 text-sm mt-6">&copy; {{ date('Y') }} SustainMonitor. All rights reserved.</p>
</div>


</body>
</html>
