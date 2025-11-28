<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="UTF-8">
<meta name="viewport" content="width=device-width, initial-scale=1.0">
<title>KDTMS | Login</title>
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

    /* Dropdown */
    .dropdown-wrapper { position: relative; }
    .dropdown-btn {
        width: 100%; padding: 1rem 0.75rem 0.25rem 0.75rem;
        border: 1px solid #d1d5db; border-radius: 0.5rem;
        text-align: left; background: white; cursor: pointer;
        transition: border-color 0.3s;
    }
    .dropdown-btn:focus { border-color: #f59e0b; }
    .dropdown-list {
        display: none; position: absolute; width: 100%;
        top: 100%; left: 0; max-height: 200px; overflow-y: auto;
        border: 1px solid #d1d5db; border-radius: 0.5rem;
        background: white; z-index: 50; margin-top: 0.25rem;
    }
    .dropdown-list div { padding: 0.75rem; cursor: pointer; }
    .dropdown-list div:hover { background: #f59e0b; color: white; }
</style>
</head>

<body class="bg-gray-100 flex items-center justify-center min-h-screen">

<div class="bg-white shadow-lg rounded-xl w-full max-w-sm p-8">
    <div class="flex justify-center mb-6">
        <img src="{{ asset('img/logo.jpg') }}" class="h-12" style="width: 220px; height: auto;">
    </div>

    <h2 class="text-2xl font-bold text-center text-gray-800 mb-6">Login to Sustain Monitor</h2>

    @if ($errors->any())
        <div class="bg-red-100 text-red-700 px-4 py-2 rounded mb-4 text-sm">
            {{ $errors->first() }}
        </div>
    @endif

    <form method="POST" action="{{ route('login.submit') }}" class="space-y-5">
        @csrf

        <!-- Email -->
        <div class="floating-input">
            <input type="email" name="email" placeholder=" " required>
            <label>Email</label>
        </div>

        <!-- Password -->
        <div class="floating-input">
            <input type="password" name="password" placeholder=" " required>
            <label>Password</label>
        </div>

        <!-- Role Dropdown -->
        <div class="floating-input dropdown-wrapper">
            <button type="button" class="dropdown-btn" id="dropdownButton">Select Role</button>
            <input type="hidden" name="role" id="roleInput" required>
            <label class="absolute top-0 left-0 text-gray-400 text-sm">Role</label>

            <div class="dropdown-list" id="dropdownList">
                <div data-value="admin">Admin</div>
                <div data-value="user">User</div>
            </div>
        </div>

        <button type="submit"
            class="w-full py-3 bg-amber-500 hover:bg-amber-600 text-white font-semibold rounded-lg shadow-sm mt-4">
            Login
        </button>
    </form>

    <p class="text-center text-sm text-gray-500 mt-5">
        Don't have an account? 
        <a href="{{ route('register.user.page') }}" class="text-blue-600 font-semibold hover:underline">Register as User</a>
        |
        <a href="{{ route('register.admin.page') }}" class="text-red-600 font-semibold hover:underline">Register as Admin</a>
    </p>

    <p class="text-center text-gray-400 text-sm mt-6">&copy; {{ date('Y') }} SustainMonitor. All rights reserved.</p>
</div>

<script>
const dropdownBtn = document.getElementById('dropdownButton');
const dropdownList = document.getElementById('dropdownList');
const roleInput = document.getElementById('roleInput');

dropdownBtn.addEventListener('click', () => {
    dropdownList.style.display = dropdownList.style.display === 'block' ? 'none' : 'block';
});

dropdownList.querySelectorAll('div').forEach(item => {
    item.addEventListener('click', () => {
        dropdownBtn.textContent = item.textContent;
        roleInput.value = item.dataset.value;
        dropdownList.style.display = 'none';
    });
});

document.addEventListener('click', (e) => {
    if (!e.target.closest('.dropdown-wrapper')) {
        dropdownList.style.display = 'none';
    }
});
</script>

</body>
</html>
