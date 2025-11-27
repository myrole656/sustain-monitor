<?php

use Illuminate\Database\Migrations\Migration;
use Illuminate\Database\Schema\Blueprint;
use Illuminate\Support\Facades\Schema;

return new class extends Migration
{
    /**
     * Run the migrations.
     */
    public function up(): void
    {
        Schema::create('sdg_status', function (Blueprint $table) {
            $table->id();
            $table->unsignedTinyInteger('sdg3')->default(0);
            $table->unsignedTinyInteger('sdg6')->default(0);
            $table->unsignedTinyInteger('sdg7')->default(0);
            $table->unsignedTinyInteger('sdg8')->default(0);
            $table->unsignedTinyInteger('sdg9')->default(0);
            $table->unsignedTinyInteger('sdg11')->default(0);
            $table->unsignedTinyInteger('sdg12')->default(0);
            $table->unsignedTinyInteger('sdg13')->default(0);
            $table->unsignedTinyInteger('sdg15')->default(0);

            // Final grade (PLATINUM/GOLD/SILVER/CERTIFIED/FAIL)
            $table->string('status')->nullable();

        // Connect to project
            $table->foreignId('project_id')->constrained('projects')->cascadeOnDelete();
            $table->timestamps();
        });
    }

    /**
     * Reverse the migrations.
     */
    public function down(): void
    {
        Schema::dropIfExists('sdg_status');
    }
};
