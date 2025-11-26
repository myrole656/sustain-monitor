<?php

use App\Http\Controllers\Auth\RegisterController;
use App\Http\Controllers\Auth\RoleLoginController;
use Illuminate\Support\Facades\Route;
use App\Http\Controllers\PDFController;
use App\Http\Controllers\GuidelineController; // <<< ADD THIS

// Home route
Route::get('/', function () {
    return redirect()->route('login');
})->name('home');

// Login
Route::get('/login', [RoleLoginController::class, 'showLoginForm'])->name('login');
Route::post('/login', [RoleLoginController::class, 'login'])->name('login.submit');
Route::post('/logout', [RoleLoginController::class, 'logout'])->name('logout');


// Registration
Route::get('/register/admin', [RegisterController::class, 'showAdminForm'])->name('register.admin.page');
Route::post('/register/admin', [RegisterController::class, 'registerAdmin'])->name('register.admin.submit');

Route::get('/register/user', [RegisterController::class, 'showUserForm'])->name('register.user.page');
Route::post('/register/user', [RegisterController::class, 'registerUser'])->name('register.user.submit');


// Admin-only
Route::middleware(['auth', 'admin'])->group(function () {
    Route::get('/admin/dashboard', function () {
        return view('admin.dashboard');
    })->name('admin.dashboard');
});


// PDF Project Route
Route::get('/pdf/project/{id}', [PDFController::class, 'projectReport'])
    ->name('pdf.project');


// ✅ ADD THESE: Guideline preview + download
Route::middleware(['auth'])->group(function () {

    Route::get('/user/guideline/preview', [GuidelineController::class, 'preview'])
        ->name('guideline.preview');

    Route::get('/user/guideline/download', [GuidelineController::class, 'download'])
        ->name('guideline.download');
});
